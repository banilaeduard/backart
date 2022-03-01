using System;

namespace DataAccess
{
    public interface IBaseEntity
    {
        DateTime CreatedDate { get; set; }
        DateTime UpdatedDate { get; set; }
    }
}
