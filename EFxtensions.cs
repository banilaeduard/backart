using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WebApi.Entities;

namespace WebApi
{
    static class EFxtensions
    {
        public static void handleCreatedUpdated(this EntityEntry entityEntry)
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

        public static void handleDataKey(this EntityEntry entityEntry, string key, bool isAdmin)
        {
            // we ensure the separation of data based on clients
            if (entityEntry.Entity is IDataKey dataKey)
            {
                if (entityEntry.State == EntityState.Added && !isAdmin)
                    dataKey.DataKey = key;
                else
                    entityEntry.Property("DataKey").IsModified = false;
            }
        }

        public static void handleTennant(this EntityEntry entityEntry, string tenant)
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
