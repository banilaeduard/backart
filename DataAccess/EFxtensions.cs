using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DataAccess
{
    static internal class EFxtensions
    {
        internal static void handleCreatedUpdated(this EntityEntry entityEntry)
        {
            if (entityEntry.Entity is IBaseEntity baseEntity)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    baseEntity.CreatedDate = System.DateTime.Now;
                    baseEntity.UpdatedDate = System.DateTime.Now;
                }
                else
                {
                    entityEntry.Property("CreatedDate").IsModified = false;
                    baseEntity.UpdatedDate = System.DateTime.Now;
                }
            }
        }

        internal static void handleDataKey(this EntityEntry entityEntry, IBaseContextAccesor accesor)
        {
            // we ensure the separation of data based on clients
            if (entityEntry.Entity is IDataKey dataKey)
            {
                if (entityEntry.State == EntityState.Added)
                    dataKey.DataKeyId = accesor.DataKeyId;
                else
                {
                    entityEntry.Property("DataKeyId").IsModified = false;
                    entityEntry.Navigation("DataKey").IsModified = false;
                }
            }
        }

        internal static void handleTennant(this EntityEntry entityEntry, string tenant)
        {
            // we ensure the separation of data based on clients
            if (entityEntry.Entity is ITenant tennant)
            {
                if (entityEntry.State == EntityState.Added)
                    tennant.TenantId = tenant;
                else
                    entityEntry.Property("TenantId").IsModified = false;
            }
        }
    }
}
