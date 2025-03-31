using Microsoft.Extensions.DependencyInjection;
using RepositoryContract.ExternalReferenceGroup;
using RepositoryContract.Tickets;
using ServiceInterface.Storage;

namespace PollerRecurringJob.JobHandlers
{
    internal static class Remove0ExternalRefsSync
    {
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            var externalRefRepository = jobContext.provider.GetService<IExternalReferenceGroupRepository>()!;
            var storageService = jobContext.provider.GetService<IStorageService>()!;

            var dateTime = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var externalRefs = await externalRefRepository.GetExternalReferences(@$"Ref_count < 1 AND TableName in ('AttachmentEntry', 'Transport') and Date <= '{dateTime}'
                ORDER BY G_Id desc
                OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
            ");

            foreach (var externalRef in externalRefs)
            {
                try
                {
                    await storageService.Delete(externalRef.ExternalGroupId);
                    ActorEventSource.Current.ActorMessage(jobContext, @$"Deleted: {externalRef.ExternalGroupId}");
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.ActorMessage(jobContext, @$"EXCEPTION POLLER: {ex.Message}. {ex.StackTrace ?? ""}");
                }
            }
            await externalRefRepository.DeleteExternalRefs([.. externalRefs.Select(x => x.G_Id)]);

            var ticketRepository = jobContext.provider.GetService<ITicketEntryRepository>()!;

            externalRefs = await externalRefRepository.GetExternalReferences(@$"Ref_count < 1 AND TableName = 'TicketEntity' and Date <= '{dateTime}'
                ORDER BY G_Id desc
                OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
            ");

            foreach (var externalRef in externalRefs)
            {
                try
                {
                    var attachments = (await ticketRepository.GetAllAttachments(externalRef.RowKey)).Where(x => x.RefPartition == externalRef.PartitionKey && x.RefKey == externalRef.RowKey).ToList();
                    if (attachments.Any())
                    {
                        foreach (var item in attachments)
                        {
                            try
                            {
                                await storageService.Delete(item.Data);
                                ActorEventSource.Current.ActorMessage(jobContext, @$"Deleted: {externalRef.ExternalGroupId}");
                            }
                            catch (Exception ex)
                            {
                                ActorEventSource.Current.ActorMessage(jobContext, @$"EXCEPTION POLLER: {ex.Message}. {ex.StackTrace ?? ""}");
                            }
                        }
                        await ticketRepository.DeleteEntity([.. attachments]);
                    }

                    await ticketRepository.DeleteEntity([await ticketRepository.GetTicket(externalRef.PartitionKey, externalRef.RowKey, externalRef.TableName)]);
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.ActorMessage(jobContext, @$"EXCEPTION POLLER: {ex.Message}. {ex.StackTrace ?? ""}");
                }
            }
            await externalRefRepository.DeleteExternalRefs([.. externalRefs.Select(x => x.G_Id)]);
        }
    }
}
