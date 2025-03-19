using EntityDto;

namespace ServiceInterface
{
    public interface ICacheManager<T> where T : ITableEntryDto
    {
        Task<IList<T>> GetAll(Func<DateTimeOffset, IList<T>> getContent, string? tableName = null);

        Task InvalidateOurs(string tableName);

        Task Bust(string tableName, bool invalidate, DateTimeOffset? stamp);

        Task BustAll();

        Task RemoveFromCache(string tableName, IList<T> entities);

        Task UpsertCache(string tableName, IList<T> entities);
    }
}
