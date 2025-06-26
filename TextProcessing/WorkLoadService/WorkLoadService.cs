using System;
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
using RepositoryContract.CommitedOrders;
using RepositoryContract.Orders;
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
                            Hash = commited.Hash,
                            Detalii = commited.Detalii,
                            DocId = commited.DocId,
                        });
                    }

                    var items = (await _structuraReport.GenerateReport(workerName, model, model)).Where(t => t.Count > 0);
                    if (items.Any())
                    {
                        model.WorkDisplayItems.AddRange(items);
                        workItems.Add(model);
                    }
                });
            }

            var orderItems = new List<WorkerPriorityList>();
            using (var tx = StateManager.CreateTransaction())
            {
                var model = new WorkerPriorityList([], "orders");
                await IterateDictionary<WorkItem>(OrdersKey, tx, (entry) =>
                {
                    model.WorkItems.Add(new WorkItem()
                    {
                        CodProdus = entry.Value.CodProdus,
                        Cantitate = entry.Value.Cantitate,
                        NumeProdus = entry.Value.NumeProdus,
                        Hash = entry.Value.Hash,
                        DocId = entry.Value.DocId,
                        Detalii = entry.Value.Detalii,
                        CodLocatie = entry.Value.CodLocatie,
                        NumarComanda = entry.Value.NumarComanda,
                        DeliveryDate = entry.Value.DeliveryDate,
                    });
                });
                var items = (await _structuraReport.GenerateReport(workerName, model, model)).Where(t => t.Count > 0);
                if (items.Any())
                {
                    model.WorkDisplayItems.AddRange(items);
                    orderItems.Add(model);
                }
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

        private async Task<List<WorkListItem>> GetWorkListItems()
        {
            List<WorkListItem> workList = null;
#if RELEASE
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable(KeyCollection.ConnectionString)))
            {
                return [.. connection.Query<WorkListItem>("SELECT * FROM dbo.TotalNelivrat")];
            }
#else
            var commited = await _provider.GetRequiredService<ICommitedOrdersRepository>().GetCommitedOrders(DateTime.MinValue);
            var orders = await _provider.GetRequiredService<IOrdersRepository>().GetOrders();

            return [..commited.Where(t => t.Cantitate > 0).Select(x => new WorkListItem() {
                Cantitate = x.Cantitate,
                CodArticol = x.CodProdus,
                CodLocatie = x.CodLocatie,
                DataDoc = x.DataDocument,
                Delivered = x.TransportDate,
                DetaliiDoc = x.DetaliiDoc,
                DetaliiLinie = x.DetaliiLinie,
                DocId = x.NumarIntern,
                NumeArticol = x.NumeProdus,
                NumeLocatie = x.NumeLocatie,
                NumarComanda = x.NumarComanda,
                DueDate = x.DueDate,
                Tip = WorkListItem.Commited,
            }), ..orders.Where(t => t.Cantitate > 0).Select(x => new WorkListItem() {
                Tip = WorkListItem.Order,
                CodArticol = x.CodArticol,
                Cantitate = x.Cantitate,
                CodLocatie = x.CodLocatie,
                DataDoc = x.DataDoc,
                DetaliiLinie = x.DetaliiLinie,
                DocId = x.DocId.ToString(),
                DueDate = x.DueDate,
                NumeArticol = x.NumeArticol,
                NumeLocatie = x.NumeLocatie,
                NumarComanda = x.NumarComanda,
                NumePartener = x.NumePartener,
                StatusName = x.StatusName
            })];
#endif
        }

        public async Task Publish()
        {
            try
            {
                _throttleCts?.Cancel();
            }
            catch (TaskCanceledException)
            {
                // Ignore if cancelled — expected during throttling
            }

            await this.PublishInternal();
        }

        public async Task PublishInternal()
        {
            List<WorkListItem> result = await GetWorkListItems();

            var tempOrdersDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, WorkItem>>(OrdersKey);
            await tempOrdersDictionary.ClearAsync();
            using (var tx = StateManager.CreateTransaction())
            {
                foreach (var order in result.Where(t => t.Tip == WorkListItem.Order && !string.IsNullOrWhiteSpace(t.CodArticol)))
                {
                    await tempOrdersDictionary.SetAsync(tx, GetWorkItemHash(order).ToString(), new WorkItem()
                    {
                        CodProdus = order.CodArticol,
                        Cantitate = order.Cantitate,
                        NumeProdus = order.NumeArticol,
                        CodLocatie = order.CodLocatie,
                        DocId = order.DocId,
                        DeliveryDate = order.DueDate,
                        Detalii = string.Join(";", order.DetaliiDoc, order.DetaliiLinie),
                        NumarComanda = order.NumarComanda,
                        Hash = GetWorkItemHash(order),
                    });
                }
                await tx.CommitAsync();
            }

            var commited = result.Where(t => t.Tip == WorkListItem.Commited && (t.TransportStatus == "Pending" || string.IsNullOrEmpty(t.TransportStatus))).ToList();
            var perDay = commited.OrderBy(x => x.Delivered).GroupBy(t => t.Delivered.HasValue ? t.Delivered.Value.ToString("dd-MM-yy") : "Pending");

            var dayDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, List<WorkItem>>>(DailyKey);
            await dayDictionary.ClearAsync();
            using (var tx = StateManager.CreateTransaction())
            {
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
                                DocId = commited.DocId,
                                Hash = GetWorkItemHash(commited),
                                Detalii =  string.Join(";", commited.DetaliiDoc, commited.DetaliiLinie)
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
            await Publish();
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

        private CancellationTokenSource _throttleCts;
        private readonly TimeSpan _throttleDelay = TimeSpan.FromMinutes(5);

        public Task ThrottlePublish(TimeSpan? timeSpan)
        {
            _throttleCts?.Cancel();
            _throttleCts = new CancellationTokenSource();
            var token = _throttleCts.Token;

            _ = ExecuteAfterDelayAsync(token, timeSpan ?? _throttleDelay);

            return Task.CompletedTask;
        }

        private async Task ExecuteAfterDelayAsync(CancellationToken token, TimeSpan span)
        {
            try
            {
                await Task.Delay(span, token); // Wait delay
                if (!token.IsCancellationRequested)
                {
                    await PublishInternal(); // Actual logic here
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore if cancelled — expected during throttling
            }
        }

        private static int GetWorkItemHash(WorkListItem item)
        {
            return GetStableHashCode(item.NumarComanda) ^ GetStableHashCode(item.CodArticol) ^ GetStableHashCode(item.CodLocatie)
                ^ GetStableHashCode(item.DetaliiDoc) ^ GetStableHashCode(item.DetaliiLinie);
        }

        private static int GetStableHashCode(string? str)
        {
            if (str == null) return 0;
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
