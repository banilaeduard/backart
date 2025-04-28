using System.Security.Cryptography;
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
                    var attachments = group.Select(t => t.Model).Select(model => new ExternalReferenceGroupEntry()
                    {
                        ExternalGroupId = model.File,
                        Id = model.TransportId,
                        PartitionKey = Environment.MachineName ?? "default",
                        RowKey = SafeSubstring(model.Md5, 40)
                    }).ToList();

                    var transportId = (int)group.First().Model.TransportId;
                    await transportRepository.HandleExternalAttachmentRefs(attachments, transportId, []);
                    var transport = await transportRepository.GetTransport(transportId);
                    //transport.Delivered = DateTime.Now.ToUniversalTime();
                    transport.CurrentStatus = "Delivered";
                    transport.TransportItems = [];
                    await transportRepository.UpdateTransport(transport, []);
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
        }

        public static string SafeSubstring(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Length > maxLength ? input.Substring(0, maxLength) : input;
        }
    }
}
