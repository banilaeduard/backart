using EntityDto;
using Microsoft.Extensions.Logging;
using ServiceInterface;
using ServiceInterface.Storage;
using System.Collections.Concurrent;

namespace ServiceImplementation.Caching
{
    // Not really local as it depends on IMetadata implementation
    public class LocalCacheManager<T> : ICacheManager<T> where T : ITableEntryDto
    {
        private IMetadataService metadataService;
        private ConcurrentDictionary<string, DateTimeOffset> lastModified = new();
        private ConcurrentDictionary<string, string?> tokens = new();
        private ConcurrentDictionary<string, ConcurrentBag<T>> cache = new();
        private Semaphore _semaphore = new(1, 1);
        private ILogger<T> logger;

        public LocalCacheManager(IMetadataService metadataService, ILogger<T> logger)
        {
            this.metadataService = metadataService;
            this.logger = logger;
            logger.LogInformation(@$"Initialized {nameof(LocalCacheManager<T>)}");
        }

        private static readonly DateTimeOffset minValueForAzure = new(2024, 1, 1, 1, 1, 1, TimeSpan.Zero);

        public async Task<IList<T>> GetAll(Func<DateTimeOffset, IList<T>> getContent, string? tableName = null)
        {
            tableName = tableName ?? typeof(T).Name;
            cache.GetOrAdd(tableName, s => []);

            var lM = lastModified.GetOrAdd(tableName, s => minValueForAzure);
            var token = tokens.GetOrAdd(tableName, s => null);

            var metaData = await metadataService.GetMetadata(GetCacheKey(tableName));
            metaData.TryGetValue("token", out var tokenSync);

            DateTimeOffset? dateSync = null;
            if (metaData.TryGetValue("timestamp", out var dSync))
            {
                dateSync = DateTimeOffset.Parse(dSync);
            }

            if (metaData.Any() && (tokenSync == null || tokenSync == token) && lM != minValueForAzure && (dateSync == null || dateSync <= lM))
                return cache[tableName].Cast<T>().ToList();
            else
            {
                using (await GetSemaphoreLock(tableName, TimeSpan.FromSeconds(45)))
                {
                    metaData = await metadataService.GetMetadata(GetCacheKey(tableName));
                    metaData.TryGetValue("token", out tokenSync);
                    dateSync = null;
                    if (metaData.TryGetValue("timestamp", out dSync))
                    {
                        dateSync = DateTimeOffset.Parse(dSync);
                    }

                    if (metaData.Any() && (tokenSync == null || tokenSync == token) && lM != minValueForAzure && (dateSync == null || dateSync <= lM))
                        return cache[tableName].Cast<T>().ToList();

                    if (tokenSync != null && tokenSync != token || !metaData.Any())
                    {
                        lM = minValueForAzure;
                        tokens.AddOrUpdate(tableName, tokenSync, (_, _) => tokenSync);
                    }

                    var content = getContent(lM);

                    if (lM == minValueForAzure) // full bust
                    {
                        var items = new ConcurrentBag<T>(content.Cast<T>());
                        if (items.Any())
                        {
                            lastModified[tableName] = items.Max(t => t.Timestamp)!.Value;
                            try
                            {
                                metaData["timestamp"] = lastModified[tableName].ToString();
                                await metadataService.SetMetadata(GetCacheKey(tableName), null, metaData);
                            }
                            catch (Exception e)
                            {
                                logger.LogError(new EventId(69), e, nameof(LocalCacheManager<T>));
                            }
                        }

                        cache[tableName] = items;
                    }
                    else
                    {
                        await UpsertCache(tableName, content.Cast<T>().ToList());
                        if (content.Any())
                        {
                            lastModified[tableName] = content.Max(t => t.Timestamp)!.Value;
                            try
                            {
                                metaData["timestamp"] = lastModified[tableName].ToString();
                                await metadataService.SetMetadata(GetCacheKey(tableName), null, metaData);
                            }
                            catch (Exception e) { logger.LogError(new EventId(69), e, nameof(LocalCacheManager<T>)); }
                        }
                    }

                    return cache[tableName].Cast<T>().ToList();
                }
            }
        }

        public async Task InvalidateOurs(string tableName)
        {
            lastModified.AddOrUpdate(tableName, minValueForAzure, (x, y) => minValueForAzure);
        }

        public async Task Bust(string tableName, bool invalidate, DateTimeOffset? stamp) // some sort of merge strategy on domain
        {
            using (await GetSemaphoreLock(tableName, TimeSpan.FromSeconds(15)))
            {
                var metaData = await metadataService.GetMetadata(GetCacheKey(tableName));
                if (invalidate)
                {
                    if (metaData.TryGetValue("token", out var tokenSync))
                    {
                        if (tokenSync != null && tokenSync != tokens.GetOrAdd(tableName, (x) => ""))
                        {
                            // means we got invalidated by some other guy. We update the token with our invalidation
                            // but we also invalidate our cache
                            lastModified.AddOrUpdate(tableName, minValueForAzure, (x, y) => minValueForAzure);
                        }
                    }
                    metaData["token"] = Guid.NewGuid().ToString();
                    tokens.AddOrUpdate(tableName, metaData["token"], (x, y) => metaData["token"]);

                    await metadataService.SetMetadata(GetCacheKey(tableName), null, metaData);
                }
                else if (metaData.TryGetValue("timestamp", out var dSync) && DateTimeOffset.TryParse(dSync, out var dateSync))
                {
                    if (stamp > dateSync)
                    {
                        metaData["timestamp"] = (stamp ?? dateSync).ToString();
                        await metadataService.SetMetadata(GetCacheKey(tableName), null, metaData);
                    }
                }
                else if (stamp.HasValue)
                {
                    metaData["timestamp"] = stamp.Value.ToString();
                    await metadataService.SetMetadata(GetCacheKey(tableName), null, metaData);
                }
            }
        }

        public async Task BustAll()
        {
            List<Task> tasks = [];
            foreach (var table in cache)
                tasks.Add(metadataService.SetMetadata(GetCacheKey(table.Key), null));
            lastModified.Clear();
            tokens.Clear();
            cache.Clear();
            await Task.WhenAll(tasks);
        }

        public async Task RemoveFromCache(string tableName, IList<T> entities)
        {
            using (await GetSemaphoreLock(tableName, TimeSpan.FromSeconds(15)))
            {
                if (entities.Any())
                {
                    var entries = cache.GetOrAdd(tableName, s => []);

                    var xcept = new ConcurrentBag<T>(entries.Except(entities, new IdentityComparer()));

                    cache.AddOrUpdate(tableName, xcept, (x, y) => xcept);
                }
            }
        }

        public async Task UpsertCache(string tableName, IList<T> entities)
        {
            using (await GetSemaphoreLock(tableName, TimeSpan.FromSeconds(15)))
            {
                if (entities.Any())
                {
                    var entries = cache.GetOrAdd(tableName, s => []);

                    var xcept = new ConcurrentBag<T>(entries.Except(entities, new IdentityComparer()));
                    foreach (var entry in entities)
                    {
                        xcept.Add(entry);
                    }

                    cache.AddOrUpdate(tableName, xcept, (x, y) => xcept);
                }
            }
        }

        private string GetCacheKey(string tableName)
        {
            return $"cache_control-{tableName}";
        }

        private async Task<WrapLock> GetSemaphoreLock(string name, TimeSpan ms)
        {
            return new WrapLock(_semaphore);
            bool timedOut = _semaphore.WaitOne(ms);

            if (timedOut)
            {
                throw new TimeoutException("The request to semaphore timed out after: " + ms);
            }
            return new WrapLock(_semaphore);
        }

        class IdentityComparer : IdentityEquality<T>, IEqualityComparer<T>
        {
            public int Id { get; set; }
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }

            public override bool Equals(T? x, T? y)
            {
                return base.Equals(x, y);
            }

            public override int GetHashCode(T obj)
            {
                return base.GetHashCode(obj);
            }
        }
    }

    class WrapLock : IDisposable
    {
        Semaphore semaphore;
        public WrapLock(Semaphore semaphore)
        {
            this.semaphore = semaphore;
        }

        public void Dispose()
        {
            //semaphore.Release(1);
            semaphore = null;
        }
    }
}
