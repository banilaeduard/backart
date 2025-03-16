namespace RepositoryContract.DataKeyLocation
{
    public interface IMainDataKeyLocationRepository
    {
        public Task<EntityDto.DataKeyLocations.DataKeyLocation> Get();
        public Task<EntityDto.DataKeyLocations.DataKeyLocation> Transfer();
    }
}
