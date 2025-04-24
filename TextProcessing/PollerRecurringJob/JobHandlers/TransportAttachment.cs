using Microsoft.Extensions.DependencyInjection;
using RepositoryContract.ExternalReferenceGroup;
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

            foreach (var group in items.GroupBy(t => t.Model?.TransportId))
            {
                try
                {
                    var attachments = group.Select(t => t.Model?.File).Select(fName => new ExternalReferenceGroupEntry()
                    {
                        ExternalGroupId = fName,
                        Id = group.First().Model.TransportId,
                        PartitionKey = Environment.MachineName ?? "default",
                        RowKey = Guid.NewGuid().ToString()
                    }).ToList();
                    await transportRepository.HandleExternalAttachmentRefs(attachments, (int)group.First().Model.TransportId, []);

                    await client.ClearWork("transportattachment", [.. group]);
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.ActorMessage(jobContext, $@"Exception : {ex.Message}. {ex.StackTrace}");
                }
            }
        }
    }
}
