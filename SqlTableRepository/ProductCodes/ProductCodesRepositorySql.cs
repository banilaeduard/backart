using Dapper;
using EntityDto;
using Microsoft.Data.SqlClient;
using RepositoryContract.ProductCodes;

namespace SqlTableRepository.ProductCodes
{
    public class ProductCodesRepositorySql : IProductCodeRepository
    {
        public Task<IList<ProductCodeStatsEntry>> CreateProductCodeStatsEntry(IList<ProductCodeStatsEntry> productStats)
        {
            throw new NotImplementedException();
        }

        public Task<IList<ProductStatsEntry>> CreateProductStats(IList<ProductStatsEntry> productStats)
        {
            throw new NotImplementedException();
        }

        public Task Delete<T>(T entity) where T : ITableEntryDto
        {
            throw new NotImplementedException();
        }

        public async Task<IList<ProductCodeEntry>> GetProductCodes(Func<ProductCodeEntry, bool> expr)
        {
            using (var conn = GetConnection())
            {
                return [.. (await conn.QueryAsync<ProductCodeEntry>(ProductCodesSql.GetProductCodes(), commandType: System.Data.CommandType.StoredProcedure)).Where(expr)];
            }
        }

        public async Task<IList<ProductCodeEntry>> GetProductCodes()
        {
            using (var conn = GetConnection())
            {
                return [.. await conn.QueryAsync<ProductCodeEntry>(ProductCodesSql.GetProductCodes(), commandType: System.Data.CommandType.StoredProcedure)];
            }
        }

        public async Task<IList<ProductCodeStatsEntry>> GetProductCodeStatsEntry()
        {
            using (var conn = GetConnection())
            {
                return [.. await conn.QueryAsync<ProductCodeStatsEntry>(ProductCodesSql.GetProductCodeStats())];
            }
        }

        public async Task<IList<ProductStatsEntry>> GetProductStats()
        {
            using (var conn = GetConnection())
            {
                return [.. await conn.QueryAsync<ProductStatsEntry>(ProductCodesSql.GetProductStats())];
            }
        }

        private SqlConnection GetConnection() => new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString"));
    }
}
