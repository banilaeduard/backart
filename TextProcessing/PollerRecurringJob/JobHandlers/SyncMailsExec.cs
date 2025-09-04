using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RepositoryContract.Tickets;
using ServiceInterface;

namespace PollerRecurringJob.JobHandlers
{
    internal static class SyncMailsExec
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string functionUrl = Environment.GetEnvironmentVariable("fetch_mail_fnc_source");
        private static CancellationTokenSource? _throttleCts;

        internal static async Task<string> Execute(PollerRecurringJob jobContext)
        {
            try
            {
                // NO NEED AS WE CHANGED TO BLOB SYNC CACHE STORAGE
                //try
                //{
                //    _throttleCts?.Cancel();
                //}
                //catch { }

                var response = await _httpClient.GetAsync(functionUrl);
                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();
                ActorEventSource.Current.ActorMessage(jobContext, result);
                //var responseObj = JsonConvert.DeserializeObject<dynamic>(result);

                //_throttleCts = new CancellationTokenSource();
                //_ = Task.Run(async () => await PollForCompletition(responseObj?.statusQueryGetUri, jobContext, _throttleCts));

                return result;
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.ActorMessage(jobContext, @$"EXCEPTION POLLER: SyncMailsExec {ex.Message}. {ex.StackTrace ?? ""}");
            }

            return "failed";
            //var proxy = ActorProxy.Create<IMailReader>(new ActorId("source1"), new Uri("fabric:/TextProcessing/MailReaderActorService"));
            //await proxy.FetchMails();
        }

        private static async Task PollForCompletition(string getStatusUrl, PollerRecurringJob jobContext, CancellationTokenSource cancellationTokenSource, int retryCount = 0, int depth = 0)
        {
            try
            {
                if (cancellationTokenSource.IsCancellationRequested) return;

                var response = await _httpClient.GetAsync(getStatusUrl);
                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();
                var responseObj = JsonConvert.DeserializeObject<dynamic>(result);

                if (cancellationTokenSource.IsCancellationRequested) return;

                if (responseObj.customStatus == "Completed" || depth > 5)
                {
                    var ticket = jobContext.provider.GetRequiredService<ICacheManager<TicketEntity>>();
                    await ticket.Bust(nameof(TicketEntity), true, null);
                    await ticket.Bust($@"{nameof(TicketEntity)}Archive", true, null);

                    var attachment = jobContext.provider.GetRequiredService<ICacheManager<AttachmentEntry>>();
                    await attachment.Bust(nameof(AttachmentEntry), true, null);
                    await attachment.Bust($@"{nameof(AttachmentEntry)}ARCHIVE", true, null);
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource = null;
                }
                else
                {
                    await Task.Delay(30 * 1000);
                    await PollForCompletition(getStatusUrl, jobContext, cancellationTokenSource, retryCount, depth++);
                }
            }
            catch (HttpRequestException ex) when (!cancellationTokenSource.IsCancellationRequested && retryCount < 4)
            {
                ActorEventSource.Current.ActorMessage(jobContext, @$"EXCEPTION PollForCompletition {ex.Message}. {ex.StackTrace ?? ""}");
                await PollForCompletition(getStatusUrl, jobContext, cancellationTokenSource, retryCount++, depth++);
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.ActorMessage(jobContext, @$"EXCEPTION PollForCompletition {ex.Message}. {ex.StackTrace ?? ""}");
                throw;
            }
        }
    }
}
