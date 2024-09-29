using AzureServices;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace AzureTableRepository
{
    internal class CacheManager<T>
    {
        static BlobAccessStorageService blobAccessStorageService = new();

        private static ConcurrentDictionary<string, DateTime> lastModified = new();
        private static ConcurrentDictionary<string, ImmutableList<T>> cache = new();
        private static ConcurrentDictionary<string, object> lockers = new();

        public static ImmutableList<T> GetAll(Func<IList<T>> getContent, string? tableName = null)
        {
            tableName = tableName ?? typeof(T).Name;
            cache.GetOrAdd(tableName, s => []);
            lastModified.GetOrAdd(tableName, s => DateTime.MinValue);
            lockers.GetOrAdd(tableName, s => new object());

            var cacheDate = blobAccessStorageService.Check($"cache_control/{tableName}");
            if (lastModified[tableName] != cacheDate)
            {
                if (lockers.TryGetValue(tableName, out var lk))
                {
                    lock (lk)
                    {
                        cacheDate = blobAccessStorageService.Check($"cache_control/{tableName}");
                        if (lastModified[tableName] != cacheDate)
                        {
                            ImmutableList<T> items = ImmutableList.CreateRange(getContent()!)!;
                            lastModified[tableName] = cacheDate;
                            cache.TryUpdate(tableName, items, cache.GetOrAdd(tableName, items));
                            return items;
                        }
                    }
                }
            }

            return cache[tableName];
        }

        public static void Bust(string? tableName = null)
        {
            tableName = tableName ?? typeof(T).Name;
            blobAccessStorageService.Bust($"cache_control/{tableName}");
        }
    }
}
