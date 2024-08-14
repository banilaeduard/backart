namespace WebApi.Models
{
    using DataAccess.Entities;
    public class FilterModel
    {
        public int Id;
        public string Query;
        public string Name;
        public string Tags;

        public static FilterModel From(Filter filterDb)
        {
            return new FilterModel()
            {
                Id = filterDb.Id,
                Query = filterDb.Query,
                Tags = filterDb.Tags,
                Name = filterDb.Name,
            };
        }

        public Filter toDatabaseModel()
        {
            return new Filter() { Id = Id, Query = Query, Tags = Tags, Name = Name };
        }
    }
}
