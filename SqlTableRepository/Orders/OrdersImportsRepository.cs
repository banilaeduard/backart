﻿using AutoMapper;
using Dapper;
using EntityDto;
using Microsoft.Data.SqlClient;
using RepositoryContract.Imports;
using Services.Storage;
using System.Text;

namespace SqlTableRepository.Orders
{
    public class OrdersImportsRepository : IImportsRepository
    {
        private IStorageService storageService;
        private IMapper mapper;

        private static MapperConfiguration config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ComandaVanzare, ComandaVanzareEntry>();
            cfg.CreateMap<ComandaVanzareEntry, ComandaVanzare>();

            cfg.CreateMap<DispozitieLivrareEntry, DispozitieLivrare>()
            .ForMember(t => t.NumarIntern, opt => opt.MapFrom(src => src.NumarIntern.ToString()));
            cfg.CreateMap<DispozitieLivrare, DispozitieLivrareEntry>()
            .ForMember(t => t.NumarIntern, opt => opt.MapFrom(src => int.Parse(src.NumarIntern)));
        });

        public OrdersImportsRepository(IStorageService storageService)
        {
            this.storageService = storageService;
            mapper = config.CreateMapper();
        }

        public async Task<(IList<DispozitieLivrare> commited, IList<ComandaVanzare> orders)> GetImportCommitedOrders(DateTime? when = null, DateTime? when2 = null)
        {
            var ro = when ?? new DateTime(2024, 9, 1);
            var ro2 = when2 ?? new DateTime(2024, 1, 1);

            var blob = storageService.Access("QImport/disp.txt", out var contentType);
            var sqlCommited = Encoding.UTF8.GetString(blob);

            var blob2 = storageService.Access("QImport/orders.txt", out var contentType2);
            var sqlOrders = Encoding.UTF8.GetString(blob2);

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("external_sql_server")))
            {
                var items = await connection.QueryMultipleAsync($"{sqlCommited} ; {sqlOrders}", new { Date1 = ro, Date2 = ro2 });
                var commited = items.Read<DispozitieLivrareEntry>();
                var orders = items.Read<ComandaVanzareEntry>();
                return (commited.Select(mapper.Map<DispozitieLivrare>).ToList(), orders.Select(mapper.Map<ComandaVanzare>).ToList());
            }
        }

        public async Task<IList<DispozitieLivrare>> GetImportCommited(DateTime? when = null)
        {
            var ro = when ?? new DateTime(2024, 9, 1);

            var blob = storageService.Access("QImport/disp.txt", out var contentType);
            var sqlCommited = Encoding.UTF8.GetString(blob);

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("external_sql_server")))
            {
                var commited = await connection.QueryAsync<DispozitieLivrareEntry>($"{sqlCommited}", new { Date1 = ro });
                return commited.Select(mapper.Map<DispozitieLivrare>).ToList();
            }
        }

        public async Task<IList<ComandaVanzare>> GetImportOrders(DateTime? when = null)
        {
            var ro = when ?? new DateTime(2024, 9, 1);

            var blob = storageService.Access("QImport/orders.txt", out var contentType);
            var sqlOrders = Encoding.UTF8.GetString(blob);

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("external_sql_server")))
            {
                var orders = await connection.QueryAsync<ComandaVanzareEntry>($"{sqlOrders}", new { Date2 = ro });
                return orders.Select(mapper.Map<ComandaVanzare>).ToList();
            }
        }

        public async Task<(DateTime commited, DateTime order)> PollForNewContent()
        {
            var blob2 = storageService.Access("QImport/poll_orders.sql", out var contentType2);
            var sqlOrders = Encoding.UTF8.GetString(blob2);

            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("external_sql_server")))
            {
                var items = await connection.QueryAsync(sqlOrders);
                var last_order = (DateTime)items.First(t => t.type == "order").updated;
                var last_commited = (DateTime)items.First(t => t.type == "commited").updated;

                return (last_commited, last_order);
            }
        }
    }
}
