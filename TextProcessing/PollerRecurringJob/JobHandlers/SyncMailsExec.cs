namespace PollerRecurringJob.JobHandlers
{
    internal static class SyncMailsExec
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string functionUrl = Environment.GetEnvironmentVariable("fetch_mail_fnc_source");

        internal static async Task Execute(PollerRecurringJob jobContext)
        {
            try
            {
                var response = await _httpClient.GetAsync(functionUrl);
                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();
                ActorEventSource.Current.Message(result);
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.ActorMessage(jobContext, @$"EXCEPTION POLLER: SyncMailsExec {ex.Message}. {ex.StackTrace ?? ""}");
            }
            //var proxy = ActorProxy.Create<IMailReader>(new ActorId("source1"), new Uri("fabric:/TextProcessing/MailReaderActorService"));
            //await proxy.FetchMails();
        }
    }
}
