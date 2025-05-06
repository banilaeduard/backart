using System.Data;
using System.Fabric;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ProjectKeys;
using RepositoryServices;
using RepositoryServices.Models;
using V2.Interfaces;

namespace WorkLoadService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class WorkLoadService : StatefulService, IWorkLoadService
    {
        internal readonly ServiceProvider _provider;
        internal readonly string cacheName = "workerItems";
        internal readonly string OrdersKey = $@"dataorders";
        internal readonly string DailyKey = $@"datadays";
        public WorkLoadService(StatefulServiceContext context, ServiceProvider provider)
            : base(context)
        {
            _provider = provider;
        }

        public async Task<Items> GetItems(string workerName)
        {
            var dictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Items>>(cacheName);
            using (var tx = StateManager.CreateTransaction())
            {
                if (await dictionary.ContainsKeyAsync(tx, workerName))
                {
                    return (await dictionary.TryGetValueAsync(tx, workerName)).Value;
                }
            }

            var _structuraReport = _provider.GetRequiredService<StructuraReportWriter>();
            var workItems = new List<WorkerPriorityList>();

            using (var tx = StateManager.CreateTransaction())
            {
                await IterateDictionary<List<WorkItem>>(DailyKey, tx, async (entries) =>
                {
                    var model = new WorkerPriorityList([], entries.Key);
                    foreach (var commited in entries.Value)
                    {
                        model.WorkItems.Add(new WorkItem()
                        {
                            CodProdus = commited.CodProdus,
                            Cantitate = commited.Cantitate,
                            CodLocatie = commited.CodLocatie,
                            DeliveryDate = commited.DeliveryDate,
                            NumarComanda = commited.NumarComanda,
                            NumeProdus = commited.NumeProdus,
                        });
                    }

                    var items = (await _structuraReport.GenerateReport(workerName, model, model)).Where(t => t.Count > 0);
                    if (items.Any())
                    {
                        model.WorkDisplayItems.AddRange(items);
                        workItems.Add(model);
                    }
                });
                await tx.CommitAsync();
            }

            var orderItems = new List<WorkerPriorityList>();
            using (var tx = StateManager.CreateTransaction())
            {
                await IterateDictionary<WorkItem>(OrdersKey, tx, async (entry) =>
                {
                    var model = new WorkerPriorityList([], "orders");
                    model.WorkItems.Add(new WorkItem()
                    {
                        CodProdus = entry.Value.CodProdus,
                        Cantitate = entry.Value.Cantitate,
                        NumeProdus = entry.Value.NumeProdus,
                    });
                    var items = (await _structuraReport.GenerateReport(workerName, model, model)).Where(t => t.Count > 0);
                    if (items.Any())
                    {
                        model.WorkDisplayItems.AddRange(items);
                        orderItems.Add(model);
                    }
                });
                await tx.CommitAsync();
            }

            var workList = new Items(workItems, orderItems);
            workList.SVC = nameof(WorkLoadService);
            using (var tx = StateManager.CreateTransaction())
            {
                await dictionary.AddOrUpdateAsync(tx, workerName, workList, (k, v) => workList);
                await tx.CommitAsync();
            }

            return workList;
        }
        

        public async Task Publish()
        {
            List<WorkListItem> result = null;
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                result = [.. await connection.QueryAsync<WorkListItem>("SELECT * FROM dbo.TotalNelivrat")];
            }


            using (var tx = StateManager.CreateTransaction())
            {
                var tempOrdersDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, WorkItem>>(OrdersKey);
                await tempOrdersDictionary.ClearAsync();
                foreach (var orderGroup in result.Where(t => t.Tip == WorkListItem.Order).GroupBy(t => t.CodArticol))
                {
                    var orderSample = orderGroup.First();
                    await tempOrdersDictionary.SetAsync(tx, orderGroup.Key, new WorkItem()
                    {
                        CodProdus = orderSample.CodArticol,
                        Cantitate = orderGroup.Sum(x => x.Cantitate),
                        NumeProdus = orderSample.NumeArticol,
                    });
                }

                var perDay = result.Where(t => t.Tip == WorkListItem.Commited && (t.TransportStatus == "Pending" || string.IsNullOrEmpty(t.TransportStatus)))
                                   .OrderBy(x => x.Delivered).GroupBy(t => t.Delivered.HasValue ? t.Delivered.Value.ToString("dd-MM-yy") : "Pending");

                var dayDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, List<WorkItem>>>(DailyKey);
                await dayDictionary.ClearAsync();
                foreach (var days in perDay)
                {
                    await dayDictionary.SetAsync(tx, days.Key, [..days.Select( commited => new WorkItem()
                            {
                                CodProdus = commited.CodArticol,
                                Cantitate = commited.Cantitate,
                                CodLocatie = commited.CodLocatie,
                                DeliveryDate = commited.Delivered,
                                NumarComanda = commited.NumarComanda,
                                NumeProdus = commited.NumeArticol,
                            })]);
                }
                await tx.CommitAsync();
            }
            var dictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Items>>(cacheName);
            await dictionary.ClearAsync();
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var dictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, Items>>(cacheName);
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return [
                  new ServiceReplicaListener((context) =>
                            new FabricTransportServiceRemotingListener(context, this))
            ];
        }

        private async Task IterateDictionary<T>(string dictName, ITransaction tx, Action<KeyValuePair<string, T>> act)
        {

            var d = await StateManager.GetOrAddAsync<IReliableDictionary<string, T>>(dictName);
            var enumerable = await d.CreateEnumerableAsync(tx);
            using (var enumerator = enumerable.GetAsyncEnumerator())
            {
                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var current = enumerator.Current;
                    act(current);
                }
            }
        }
    }
}
