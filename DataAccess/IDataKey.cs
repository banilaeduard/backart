using DataAccess.Entities;

namespace DataAccess
{
    public interface IDataKey
    {
        string DataKeyId { get; set; }
        DataKeyLocation DataKey { get; set; }
    }
}
