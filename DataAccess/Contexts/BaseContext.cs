using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Context
{
    public abstract class BaseContext : DbContext
    {
        protected IBaseContextAccesor baseContextAccesor;
        public BaseContext(DbContextOptions ctxBuilder,
            IBaseContextAccesor baseContextAccesor) : base(ctxBuilder)
        {
            this.baseContextAccesor = baseContextAccesor;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            bool ignored = false;

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var props = entity.ClrType.GetProperties().Where(t => t.PropertyType == typeof(bool) && t.CanWrite);
                foreach (var e in props) {
                    modelBuilder.Entity(entity.ClrType)
                           .Property(e.Name).HasConversion<System.Int16>();
                }
            }

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Where(e => e.ClrType.GetInterface(typeof(IDataKey).Name) != null))
            {
                if (!ignored)
                {
                    modelBuilder.Entity<DataKeyLocation>().ToTable("DataKeyLocation", t => t.ExcludeFromMigrations());
                    ignored = true;
                }

                modelBuilder.Entity(entityType.ClrType)
                            .HasOne("DataKey")
                            .WithMany()
                            .HasForeignKey("DataKeyId")
                            .OnDelete(DeleteBehavior.Restrict);

                modelBuilder.Entity(entityType.ClrType)
                            .Navigation("DataKey")
                            .AutoInclude();
            }

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Where(e => e.ClrType.GetInterface(typeof(ITenant).Name) != null
                            || e.ClrType.GetInterface(typeof(IDataKey).Name) != null
                            || e.ClrType.GetInterface(typeof(ISoftDelete).Name) != null))
            {
                var baseFilter = (Expression<Func<Object, bool>>)(_ => baseContextAccesor.disableFiltering);
                var tenantFilter = (Expression<Func<ITenant, bool>>)(e => e.TenantId == baseContextAccesor.TenantId);
                var dataKeyFilter = (Expression<Func<IDataKey, bool>>)(e => e.DataKey.locationCode == baseContextAccesor.DataKeyLocation ||
                                                                            e.DataKey.Id == baseContextAccesor.DataKeyId ||
                                                                            baseContextAccesor.DataKeyLocation == "admin");
                var isAdminDataKey = (Expression<Func<IDataKey, bool>>)(e => baseContextAccesor.IsAdmin);
                var softDeleted = (Expression<Func<ISoftDelete, bool>>)(e => e.isDeleted != true);

                var filters = new List<LambdaExpression>();

                if (typeof(ITenant).IsAssignableFrom(entityType.ClrType))
                    filters.Add(tenantFilter);
                if (typeof(IDataKey).IsAssignableFrom(entityType.ClrType))
                    filters.Add(CombineQueryFilters(entityType.ClrType, isAdminDataKey, new List<LambdaExpression>() { dataKeyFilter }));
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                    filters.Add(softDeleted);
                if (filters.Count > 0)
                {
                    var queryFilter = CombineQueryFilters(entityType.ClrType, baseFilter, filters);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(queryFilter);
                }
            }

            base.OnModelCreating(modelBuilder);
        }

        private LambdaExpression CombineQueryFilters(Type entityType, LambdaExpression baseFilter, IEnumerable<LambdaExpression> andAlsoExpressions)
        {
            var newParam = Expression.Parameter(entityType);

            var andAlsoExprBase = (Expression<Func<object, bool>>)(_ => true);
            var andAlsoExpr = ReplacingExpressionVisitor.Replace(andAlsoExprBase.Parameters.Single(), newParam, andAlsoExprBase.Body);
            foreach (var expressionBase in andAlsoExpressions)
            {
                var expression = ReplacingExpressionVisitor.Replace(expressionBase.Parameters.Single(), newParam, expressionBase.Body);
                andAlsoExpr = Expression.AndAlso(andAlsoExpr, expression);
            }

            var baseExp = ReplacingExpressionVisitor.Replace(baseFilter.Parameters.Single(), newParam, baseFilter.Body);
            var exp = Expression.OrElse(baseExp, andAlsoExpr);

            return Expression.Lambda(exp, newParam);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            addDataContext(ChangeTracker);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            addDataContext(ChangeTracker);
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void addDataContext(ChangeTracker ChangeTracker)
        {
            var correlationId = Guid.NewGuid().ToString();
            foreach (var entityEntry in ChangeTracker.Entries().ToList())
            {
                BeforeSave(entityEntry, correlationId);
                if (!baseContextAccesor.disableFiltering)
                {
                    entityEntry.handleCreatedUpdated();
                    entityEntry.handleDataKey(baseContextAccesor);
                    entityEntry.handleTennant(baseContextAccesor.TenantId);
                }
            }
        }

        protected abstract void BeforeSave(EntityEntry entityEntry, string correlationId);
    }
}
