using Azure;
using Azure.Data.Tables;
using AzureServices;
using RepositoryContract;
using System.Collections.Concurrent;

namespace AzureTableRepository
{
    internal class CacheManager
    {
        private static ConcurrentDictionary<string, DateTimeOffset> lastModified = new();
        private static ConcurrentDictionary<string, string?> tokens = new();
        private static ConcurrentDictionary<string, ConcurrentBag<ITableEntity>> cache = new();
        private static ConcurrentDictionary<string, object> lockers = new();

        private static volatile bool syncing;
        private static readonly DateTimeOffset minValueForAzure = new(2024, 1, 1, 1, 1, 1, TimeSpan.Zero);

        public static IList<T> GetAll<T>(Func<DateTimeOffset, IList<T>> getContent, string tableName = null) where T : ITableEntity
        {
            BlobAccessStorageService blobAccessStorageService = new();
            tableName = tableName ?? typeof(T).Name;
            cache.GetOrAdd(tableName, s => []);

            var lM = lastModified.GetOrAdd(tableName, s => minValueForAzure);
            var lk = lockers.GetOrAdd(tableName, s => new object());
            var token = tokens.GetOrAdd(tableName, s => null);

            var metaData = blobAccessStorageService.GetMetadata($"cache_control/{tableName}");
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
                syncing = true;
                lock (lk)
                {
                    try
                    {
                        syncing = true;
                        metaData = blobAccessStorageService.GetMetadata($"cache_control/{tableName}");
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
                            var items = new ConcurrentBag<ITableEntity>(content.Cast<ITableEntity>());
                            if (items.Any())
                            {
                                lastModified[tableName] = items.Max(t => t.Timestamp)!.Value;
                                try
                                {
                                    metaData["timestamp"] = lastModified[tableName].ToString();
                                    blobAccessStorageService.SetMetadata($"cache_control/{tableName}", null, metaData);
                                }
                                catch (Exception e) { }
                            }

                            cache[tableName] = items;
                        }
                        else
                        {
                            UpsertCache(tableName, content.Cast<ITableEntity>().ToList());
                            if (content.Any())
                            {
                                lastModified[tableName] = content.Max(t => t.Timestamp)!.Value;
                                try
                                {
                                    metaData["timestamp"] = lastModified[tableName].ToString();
                                    blobAccessStorageService.SetMetadata($"cache_control/{tableName}", null, metaData);
                                }
                                catch (Exception e) { }
                            }
                        }

                        return cache[tableName].Cast<T>().ToList();
                    }
                    finally
                    {
                        syncing = false;
                    }
                }
            }
        }

        public static void InvalidateOurs(string tableName)
        {
            lastModified.AddOrUpdate(tableName, minValueForAzure, (x, y) => minValueForAzure);
        }

        public static void Bust(string tableName, bool invalidate, DateTimeOffset? stamp) // some sort of merge strategy on domain
        {
            lock (lastModified)
            {
                BlobAccessStorageService blobAccessStorageService = new();

                var lease = blobAccessStorageService.GetLease($"cache_control/{tableName}");
                lease.Acquire(TimeSpan.FromSeconds(15), new RequestConditions());

                var metaData = blobAccessStorageService.GetMetadata($"cache_control/{tableName}");
                try
                {
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

                        blobAccessStorageService.SetMetadata($"cache_control/{tableName}", lease.LeaseId, metaData);
                    }
                    else if (metaData.TryGetValue("timestamp", out var dSync) && DateTimeOffset.TryParse(dSync, out var dateSync))
                    {
                        if (stamp > dateSync)
                        {
                            metaData["timestamp"] = (stamp ?? dateSync).ToString();
                            blobAccessStorageService.SetMetadata($"cache_control/{tableName}", lease.LeaseId, metaData);
                        }
                    }
                    else if (stamp.HasValue)
                    {
                        metaData["timestamp"] = stamp.Value.ToString();
                        blobAccessStorageService.SetMetadata($"cache_control/{tableName}", lease.LeaseId, metaData);
                    }
                }
                finally
                {
                    lease.Release();
                }
            }
        }

        public static void RemoveFromCache(string tableName, IList<ITableEntity> entities)
        {
            if (syncing)
                lock (lockers.GetOrAdd(tableName, s => new object())) { }
            if (entities.Any())
            {
                var entries = cache.GetOrAdd(tableName, s => []);

                var xcept = new ConcurrentBag<ITableEntity>(entries.Except(entities, new TableEntityPK()));

                cache.AddOrUpdate(tableName, xcept, (x, y) => xcept);
            }
        }

        public static void UpsertCache(string tableName, IList<ITableEntity> entities)
        {
            if (syncing)
                lock (lockers.GetOrAdd(tableName, s => new object())) { }
            if (entities.Any())
            {
                var entries = cache.GetOrAdd(tableName, s => []);

                entries.Except(entities, new TableEntityPK());
                var xcept = new ConcurrentBag<ITableEntity>(entries.Except(entities, new TableEntityPK()));
                foreach (var entry in entities)
                {
                    xcept.Add(entry);
                }

                cache.AddOrUpdate(tableName, xcept, (x, y) => xcept);
            }
        }
    }
}
