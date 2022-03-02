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
            var result = service.Events.Instances(appSettings.calendarid, "complaintseries" + id).Execute();
            return Task.FromResult(result.Items?.Count > 0 ? false : true);
        }

        public async Task process(ComplaintSeries message, string id)
        {
            var myEvent = new Event()
            {
                Id = "complaintseries" + id,
                Summary = message.DataKey,
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
            catch (Exception)
            {
                /*try
                {
                    service.Events.Update(myEvent, appSettings.calendarid, myEvent.Id).Execute();
                    Console.WriteLine("Insert/Update new Event ");
                    Console.Read();

                }
                catch (Exception)
                {
                    Console.WriteLine("can't Insert/Update new Event ");

                }*/
            }
        }

        public void Dispose()
        {
            service.Dispose();
        }
    }
}