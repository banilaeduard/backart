using EntityDto;
using Microsoft.Extensions.DependencyInjection;
using RepositoryContract;
using RepositoryContract.ExternalReferenceGroup;
using RepositoryContract.Tasks;
using RepositoryContract.Tickets;
using RepositoryContract.Transports;
using ServiceInterface.Storage;

namespace PollerRecurringJob.JobHandlers
{
    internal static class TransportAttachment
    {
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            IWorkflowTrigger client = jobContext.provider.GetRequiredService<IWorkflowTrigger>();
            var items = await client.GetWork<dynamic>("transportattachment");

            ITransportRepository transportRepository = jobContext.provider.GetRequiredService<ITransportRepository>();
            ITaskRepository taskRepository = jobContext.provider.GetRequiredService<ITaskRepository>();


            foreach (var group in items.GroupBy(t => t.Model?.TransportId))
            {
                try
                {
                    var attachments = group.Select(t => t.Model).Select(model => new ExternalReferenceGroupEntry()
                    {
                        ExternalGroupId = model.File,
                        Id = model.TransportId,
                        PartitionKey = Environment.MachineName ?? "default",
                        RowKey = model.Md5
                    }).ToList();

                    var transportId = (int)group.First().Model.TransportId;
                    await transportRepository.HandleExternalAttachmentRefs(attachments, transportId, []);
                    var transport = await transportRepository.GetTransport(transportId);
                    //transport.Delivered = DateTime.Now.ToUniversalTime();
                    transport.CurrentStatus = "Delivered";
                    var ti = transport.TransportItems;
                    transport.TransportItems = [];
                    await transportRepository.UpdateTransport(transport, []);

                    var taskIds = ti?.Where(t => t.DocumentType == 2).Select(x => int.Parse(x.ExternalItemId)).ToArray();
                    if (taskIds?.Any() == true)
                    {
                        await taskRepository.MarkAsClosed(taskIds);
                        var tasks = await taskRepository.GetTasks(taskIds);

                        await client.Trigger("movemailto", new MoveToMessage<TableEntityPK>
                        {
                            DestinationFolder = "Archive",
                            Items = tasks.SelectMany(x => x.ExternalReferenceEntries).Select(x => TableEntityPK.From(x.PartitionKey!, x.RowKey!)).ToList()
                        });

                        await client.Trigger("archivemail",
                           tasks.SelectMany(x => x.ExternalReferenceEntries).Select(x => new ArchiveMail()
                           {
                               FromTable = nameof(TicketEntity),
                               ToTable = $@"{nameof(TicketEntity)}Archive",
                               PartitionKey = x.PartitionKey,
                               RowKey = x.RowKey,
                           }).ToList()
                        );
                    }
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.ActorMessage(jobContext, $@"Exception : {ex.Message}. {ex.StackTrace}");
                    foreach (var item in group)
                        await client.Trigger<dynamic>("transportattachmentpoison", item.Model);
                }
                finally
                {
                    await client.ClearWork("transportattachment", [.. group]);
                }
            }

            await ArchiveMails.Execute(jobContext);
        }
    }
}
