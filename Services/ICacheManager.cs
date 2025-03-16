using EntityDto;

namespace ServiceInterface
{
    public interface ICacheManager<T> where T : ITableEntryDto<T>
    {
        Task<IList<T>> GetAll(Func<DateTimeOffset, IList<T>> getContent, string? tableName = null);

        void InvalidateOurs(string tableName);

        Task Bust(string tableName, bool invalidate, DateTimeOffset? stamp);

        Task BustAll();

        void RemoveFromCache(string tableName, IList<T> entities);

        void UpsertCache(string tableName, IList<T> entities);
    }
}
