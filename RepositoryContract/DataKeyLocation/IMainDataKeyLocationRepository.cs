namespace RepositoryContract.DataKeyLocation
{
    public interface IMainDataKeyLocationRepository
    {
        public Task<DataKeyLocationBase> Get();
        public Task<DataKeyLocationBase> Transfer();
    }
}
