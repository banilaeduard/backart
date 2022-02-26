using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Entities
{
    public abstract class BaseContext : DbContext
    {
        public IHttpContextAccessor httpContextAccessor;

        private bool filtersDisabled = false;
        public BaseContext(DbContextOptions ctxBuilder,
            IHttpContextAccessor httpContextAccessor) : base(ctxBuilder)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Where(e => e.ClrType.GetInterface(typeof(ITenant).Name) != null
                            || e.ClrType.GetInterface(typeof(IDataKey).Name) != null))
            {
                var baseFilter = (Expression<Func<Object, bool>>)(_ => filtersDisabled);
                var tenantFilter = (Expression<Func<ITenant, bool>>)(e => e.TenantId == httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Actor).Value);
                var dataKeyFilter = (Expression<Func<IDataKey, bool>>)(e => e.DataKey == httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.GivenName).Value);
                var isAdminDataKey = (Expression<Func<IDataKey, bool>>)(e => httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role).Value == "admin");

                var filters = new List<LambdaExpression>();

                if (typeof(ITenant).IsAssignableFrom(entityType.ClrType))
                    filters.Add(tenantFilter);
                if (typeof(IDataKey).IsAssignableFrom(entityType.ClrType))
                    filters.Add(CombineQueryFilters(entityType.ClrType, isAdminDataKey, new List<LambdaExpression>() { dataKeyFilter }));

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

            var andAlsoExprBase = (Expression<Func<Object, bool>>)(_ => true);
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
            var tenant = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Actor).Value;
            var dataKey = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.GivenName).Value;
            bool isAdmin = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value == "admin";

            foreach (var entityEntry in ChangeTracker.Entries().ToList())
            {
                BeforeSave(entityEntry);
                entityEntry.handleCreatedUpdated();
                entityEntry.handleDataKey(dataKey, isAdmin);
                entityEntry.handleTennant(tenant);
            }
        }

        protected abstract void BeforeSave(EntityEntry entityEntry);
    }
}