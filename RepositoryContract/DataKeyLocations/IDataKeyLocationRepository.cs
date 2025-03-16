namespace RepositoryContract.DataKeyLocation
{
    public interface IDataKeyLocationRepository
    {
        Task<IList<DataKeyLocationEntry>> GetLocations();
        Task UpdateLocation(DataKeyLocationEntry[] entry);
        Task DeleteLocation(DataKeyLocationEntry[] entry);
        Task<DataKeyLocationEntry[]> InsertLocation(DataKeyLocationEntry[] entry);
    }
}
