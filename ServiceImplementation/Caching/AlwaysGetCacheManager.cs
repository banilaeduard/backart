using EntityDto;
using ServiceInterface;
using ServiceInterface.Storage;

namespace ServiceImplementation.Caching
{
    /// <summary>
    /// We use this for the passthrough in some situations. We still implement the BUST because it helps other caches use the metadata provided
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AlwaysGetCacheManager<T> : ICacheManager<T> where T : ITableEntryDto
    {
        private static readonly DateTimeOffset minValueForAzure = new(2024, 1, 1, 1, 1, 1, TimeSpan.Zero);
        private IMetadataService metadataService;

        public AlwaysGetCacheManager(IMetadataService metadataService)
        {
            this.metadataService = metadataService;
        }
        private string GetCacheKey(string tableName)
        {
            return $"cache_control-{tableName}";
        }
        public async Task Bust(string tableName, bool invalidate, DateTimeOffset? stamp)
        {
            var metaData = await metadataService.GetMetadata(GetCacheKey(tableName));
            if (invalidate)
            {
                metaData["token"] = Guid.NewGuid().ToString();
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

        public async Task BustAll()
        {
            //no op
        }

        public async Task<IList<T>> GetAll(Func<DateTimeOffset, IList<T>> getContent, string? tableName = null)
        {
            return getContent(minValueForAzure);
        }

        public async Task InvalidateOurs(string tableName)
        {
            // no op
        }

        public async Task RemoveFromCache(string tableName, IList<T> entities)
        {
        }

        public async Task UpsertCache(string tableName, IList<T> entities)
        {
            // no op
        }
    }
}
