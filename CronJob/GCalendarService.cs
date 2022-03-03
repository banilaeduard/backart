using core;
using DataAccess.Entities;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CronJob
{
    internal class GCalendarServiceProcessor : IProcessor<ComplaintSeries>, IDisposable
    {
        private AppSettings appSettings;
        private CalendarService service;
        private string eventIdFormat = "calendarcomplaintseries{0}";
        public GCalendarServiceProcessor(AppSettings appSettings)
        {
            this.appSettings = appSettings;
            var credential = new ServiceAccountCredential(
                               new ServiceAccountCredential.Initializer(appSettings.gcalendaruser, "https://oauth2.googleapis.com/token")
                               {
                                   User = appSettings.calendarid,
                                   Scopes = new[] { CalendarService.Scope.Calendar, CalendarService.Scope.CalendarEvents }
                               }.FromPrivateKey(appSettings.gcalendarkey));
            service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Calendar API",
            });
        }

        public Task<bool> shouldProcess(ComplaintSeries message, string id)
        {
            var result = service.Events.Instances(appSettings.calendarid, string.Format(eventIdFormat, id)).Execute();
            return Task.FromResult(result.Items?.Count <= 0 && message.Status == Constants.COMPLAINT_SUCCESS
                || result.Items?.Count > 0 && message.Status == Constants.COMPLAINT_REJECT);
        }

        public async Task process(ComplaintSeries message, string id)
        {
            if (message.Status == Constants.COMPLAINT_REJECT)
            {
                try
                {
                    service.Events.Delete(appSettings.calendarid, string.Format(eventIdFormat, id)).Execute();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                var myEvent = new Event()
                {
                    Id = string.Format(eventIdFormat, id),
                    Summary = message.DataKey?.locationCode,
                    Location = "Reclamatii",
                    Description = message.Tickets[0]?.Description,
                    Start = new EventDateTime()
                    {
                        Date = message.CreatedDate.ToString("yyyy-MM-dd")
                    },
                    End = new EventDateTime()
                    {
                        Date = message.CreatedDate.ToString("yyyy-MM-dd")
                    },
                    Recurrence = new List<string> { "RRULE:FREQ=DAILY;COUNT=14" },
                    Attachments = new List<EventAttachment>()
                };
                foreach (var attachment in message.Tickets[0]?.Images)
                {
                    myEvent.Attachments.Add(new EventAttachment()
                    {
                        Title = attachment.Title,
                        FileUrl = attachment.Data
                    });
                }
                var InsertRequest = service.Events.Insert(myEvent, appSettings.calendarid);

                try
                {
                    myEvent = InsertRequest.Execute();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void Dispose()
        {
            service.Dispose();
        }
    }
}