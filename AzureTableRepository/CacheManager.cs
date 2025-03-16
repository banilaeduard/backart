using EntityDto;
using ServiceInterface;
using ServiceInterface.Storage;
using System.Collections.Concurrent;

namespace AzureTableRepository
{
    public class CacheManager<T>: ICacheManager<T> where T: ITableEntryDto<T>
    {
        private static readonly SemaphoreSlim _semaphoreSlim = new(0, 1);
        private IMetadataService metadataService;
        private ConcurrentDictionary<string, DateTimeOffset> lastModified = new();
        private ConcurrentDictionary<string, string?> tokens = new();
        private ConcurrentDictionary<string, ConcurrentBag<T>> cache = new();

        public CacheManager(IMetadataService metadataService)
        {
            this.metadataService = metadataService;
        }

        private static readonly DateTimeOffset minValueForAzure = new(2024, 1, 1, 1, 1, 1, TimeSpan.Zero);

        public async Task<IList<T>> GetAll(Func<DateTimeOffset, IList<T>> getContent, string? tableName = null)
        {
            tableName = tableName ?? typeof(T).Name;
            cache.GetOrAdd(tableName, s => []);

            var lM = lastModified.GetOrAdd(tableName, s => minValueForAzure);
            var token = tokens.GetOrAdd(tableName, s => null);

            var metaData = await metadataService.GetMetadata($"cache_control_{tableName}");
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
                using (await GetSemaphore(tableName).Acquire(TimeSpan.FromSeconds(15)))
                {
                    metaData = await metadataService.GetMetadata($"cache_control-{tableName}");
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
                                await metadataService.SetMetadata($"cache_control-{tableName}", null, metaData);
                            }
                            catch (Exception e) { }
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
                                await metadataService.SetMetadata($"cache_control-{tableName}", null, metaData);
                            }
                            catch (Exception e) { }
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
            using (await GetSemaphore(tableName).Acquire(TimeSpan.FromSeconds(15)))
            {
                using (var lease = await metadataService.GetLease($"cache_control-{tableName}"))
                {
                    await lease.Acquire(TimeSpan.FromSeconds(15));

                    var metaData = await metadataService.GetMetadata($"cache_control-{tableName}");
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

                        await metadataService.SetMetadata($"cache_control-{tableName}", lease.LeaseId, metaData);
                    }
                    else if (metaData.TryGetValue("timestamp", out var dSync) && DateTimeOffset.TryParse(dSync, out var dateSync))
                    {
                        if (stamp > dateSync)
                        {
                            metaData["timestamp"] = (stamp ?? dateSync).ToString();
                            await metadataService.SetMetadata($"cache_control-{tableName}", lease.LeaseId, metaData);
                        }
                    }
                    else if (stamp.HasValue)
                    {
                        metaData["timestamp"] = stamp.Value.ToString();
                        await metadataService.SetMetadata($"cache_control-{tableName}", lease.LeaseId, metaData);
                    }
                }
            }
        }

        public async Task BustAll()
        {
            List<Task> tasks = [];
            foreach (var table in cache)
                tasks.Add(metadataService.SetMetadata($@"cache_control-{table.Key}", null));
            lastModified.Clear();
            tokens.Clear();
            cache.Clear();
            await Task.WhenAll(tasks);
        }

        public async Task RemoveFromCache(string tableName, IList<T> entities)
        {
            using (await GetSemaphore(tableName).Acquire(TimeSpan.FromSeconds(15)))
            {
                if (entities.Any())
                {
                    var entries = cache.GetOrAdd(tableName, s => []);

                    var xcept = new ConcurrentBag<T>(entries.Except(entities));

                    cache.AddOrUpdate(tableName, xcept, (x, y) => xcept);
                }
            }
        }

        public async Task UpsertCache(string tableName, IList<T> entities)
        {
            using (await GetSemaphore(tableName).Acquire(TimeSpan.FromSeconds(15))) { }
            if (entities.Any())
            {
                var entries = cache.GetOrAdd(tableName, s => []);

                entries.Except(entities);
                var xcept = new ConcurrentBag<T>(entries.Except(entities));
                foreach (var entry in entities)
                {
                    xcept.Add(entry);
                }

                cache.AddOrUpdate(tableName, xcept, (x, y) => xcept);
            }
        }

        private static WrapLock GetSemaphore(string name)
        {
            return new(_semaphoreSlim);
        }
    }

    class WrapLock : IDisposable
    {
        SemaphoreSlim semaphore;
        public WrapLock(SemaphoreSlim semaphore)
        {
            this.semaphore = semaphore;
        }

        public async Task<WrapLock> Acquire(TimeSpan ms)
        {
            await semaphore.WaitAsync(ms);
            return this;
        }

        public void Dispose()
        {
            semaphore.Release();
            semaphore = null;
        }
    }
}
