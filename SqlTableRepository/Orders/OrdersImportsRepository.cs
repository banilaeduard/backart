using AutoMapper;
using Dapper;
using EntityDto.CommitedOrders;
using Microsoft.Data.SqlClient;
using ProjectKeys;
using RepositoryContract.Imports;
using ServiceInterface.Storage;

namespace SqlTableRepository.Orders
{
    public class OrdersImportsRepository<T> : IImportsRepository where T : IStorageService
    {
        private IStorageService storageService;
        private IMapper mapper;

        private static MapperConfiguration config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Order, ComandaVanzareEntry>();
            cfg.CreateMap<ComandaVanzareEntry, Order>();

            cfg.CreateMap<DispozitieLivrareEntry, CommitedOrder>()
            .ForMember(t => t.NumarIntern, opt => opt.MapFrom(src => src.NumarIntern.ToString()));
            cfg.CreateMap<CommitedOrder, DispozitieLivrareEntry>()
            .ForMember(t => t.NumarIntern, opt => opt.MapFrom(src => int.Parse(src.NumarIntern)));
        });

        public OrdersImportsRepository(T storageService)
        {
            this.storageService = storageService;
            mapper = config.CreateMapper();
        }

        public async Task<(IList<CommitedOrder> commited, IList<Order> orders)> GetImportCommitedOrders(DateTime? when = null, DateTime? when2 = null)
        {
            var ro = when ?? new DateTime(2024, 9, 1);
            var ro2 = when2 ?? new DateTime(2024, 1, 1);
            string sqlCommited = string.Empty;
            string sqlOrders = string.Empty;

            using (var stream = storageService.Access("QImport/disp.txt", out var contentType))
            using (var streamReadear = new StreamReader(stream))
                sqlCommited = streamReadear.ReadToEnd();

            using (var stream = storageService.Access("QImport/orders.txt", out var contentType))
            using (var streamReadear = new StreamReader(stream))
                sqlOrders = streamReadear.ReadToEnd();

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ExternalServer)))
            {
                var items = await connection.QueryMultipleAsync($"{sqlCommited} ; {sqlOrders}", new { Date1 = ro, Date2 = ro2 });
                var commited = items.Read<DispozitieLivrareEntry>();
                var orders = items.Read<ComandaVanzareEntry>();
                return (commited.Select(mapper.Map<CommitedOrder>).ToList(), orders.Select(mapper.Map<Order>).ToList());
            }
        }

        public async Task<IList<CommitedOrder>> GetImportCommited(DateTime? when = null)
        {
            var ro = when ?? new DateTime(2024, 9, 1);
            string sqlCommited = string.Empty;

            using (var stream = storageService.Access("QImport/disp.txt", out var contentType))
            using (var streamReadear = new StreamReader(stream))
                sqlCommited = streamReadear.ReadToEnd();

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ExternalServer)))
            {
                var commited = await connection.QueryAsync<DispozitieLivrareEntry>($"{sqlCommited}", new { Date1 = ro });
                return commited.Select(mapper.Map<CommitedOrder>).ToList();
            }
        }

        public async Task<IList<Order>> GetImportOrders(DateTime? when = null)
        {
            var ro = when ?? new DateTime(2024, 9, 1);
            string sqlOrders = string.Empty;

            using (var stream = storageService.Access("QImport/orders.txt", out var contentType))
            using (var streamReadear = new StreamReader(stream))
                sqlOrders = streamReadear.ReadToEnd();

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ExternalServer)))
            {
                var orders = await connection.QueryAsync<ComandaVanzareEntry>($"{sqlOrders}", new { Date2 = ro });
                return orders.Select(mapper.Map<Order>).ToList();
            }
        }

        public async Task<(DateTime commited, DateTime order)> PollForNewContent()
        {
            string sqlOrders = string.Empty;
            using (var stream = storageService.Access("QImport/poll_orders.sql", out var contentType))
            using (var streamReadear = new StreamReader(stream))
                sqlOrders = streamReadear.ReadToEnd();

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ExternalServer)))
            {
                var items = await connection.QueryAsync(sqlOrders);
                var last_order = (DateTime)items.First(t => t.type == "order").updated;
                var last_commited = (DateTime)items.First(t => t.type == "commited").updated;

                return (last_commited, last_order);
            }
        }
    }
}
