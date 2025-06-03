using Microsoft.Extensions.DependencyInjection;
using RepositoryContract.Tickets;
using ServiceInterface.Storage;

namespace PollerRecurringJob.JobHandlers
{
    internal static class RemoveLostAttachments
    {
        static readonly string AttachTempArchive = $"{nameof(AttachmentEntry)}ARCHIVE";
        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            var ticketRepository = jobContext.provider.GetRequiredService<ITicketEntryRepository>()!;
            var storageService = jobContext.provider.GetRequiredService<IStorageService>()!;

            var tickets = await ticketRepository.GetAll();
            var ticketsArchived = await ticketRepository.GetAll($@"{nameof(TicketEntity)}Archive");

            var allAttach = await ticketRepository.GetAllAttachments();
            var allArchiveAttach = await ticketRepository.GetAllAttachments(null, AttachTempArchive);

            var allAttachments = allAttach.Where(x => !tickets.Any(t => t.PartitionKey == x.RefPartition && t.RowKey == x.RefKey)).Take(20).ToList();
            var allAttachments2 = allArchiveAttach.Where(x => (!x.IsDeleted.HasValue || x.IsDeleted == false) && !tickets.Any(t => t.PartitionKey == x.RefPartition && t.RowKey == x.RefKey)).Take(20).ToList();

            foreach (var attach in (AttachmentEntry[])[.. allAttachments, .. allAttachments2])
            {
                try
                {
                    await storageService.Delete(attach.Data);
                    attach.IsDeleted = true;
                    ActorEventSource.Current.ActorMessage(jobContext, @$"RemoveLostAttachments Deleted: {attach.Data}");
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.ActorMessage(jobContext, @$"EXCEPTION POLLER: RemoveLostAttachments {ex.Message}. {ex.StackTrace ?? ""}");
                }

            }
            await ticketRepository.DeleteEntity([.. allAttachments]);
            await ticketRepository.Save([.. allAttachments, .. allAttachments2], AttachTempArchive);

            var deleted = allArchiveAttach.Where(x => x.IsDeleted.HasValue && x.IsDeleted == true && !ticketsArchived.Any(t => t.PartitionKey == x.RefPartition && t.RowKey == x.RefKey)).Take(99);
            await ticketRepository.DeleteEntity([.. deleted], null, AttachTempArchive);
        }
    }
}
