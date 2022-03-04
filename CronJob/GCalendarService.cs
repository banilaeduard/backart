using core;
using DataAccess.Entities;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CronJob
{
    internal class GCalendarServiceProcessor : IProcessor<ComplaintSeries>, IDisposable
    {
        private AppSettings appSettings;
        private CalendarService service;
        private string eventIdFormat = "casalcosdmpserq{0}";
        private Regex regex = new Regex(@"\r\n?|\n|<p>|</p>");
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
            return Task.FromResult(
                (message.Status != Constants.COMPLAINT_SUCCESS || message.isDeleted) && result.Items.Count > 0
                ||
                message.Status == Constants.COMPLAINT_SUCCESS && !message.isDeleted && result.Items.Count == 0);
        }

        public async Task process(ComplaintSeries message, string id)
        {
            if (message.Status == Constants.COMPLAINT_REJECT
                || message.isDeleted)
            {
                try
                {
                    var test = service.Events.Delete(appSettings.calendarid, string.Format(eventIdFormat, id)).Execute();
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
                    Location = "Targoviste, Romania",
                    Description = regex.Replace(message.Tickets[0]?.Description, " "),
                    Start = new EventDateTime()
                    {
                        Date = DateTime.Now.ToString("yyyy-MM-dd")
                    },
                    End = new EventDateTime()
                    {
                        Date = DateTime.Now.ToString("yyyy-MM-dd")
                    },
                    Recurrence = new List<string>
                    { String.Format("RRULE:FREQ=DAILY;UNTIL={0};INTERVAL=2", DateTime.Now.AddDays(8).ToString("yyyyMMdd")) },
                    Attachments = new List<EventAttachment>(),
                    ColorId = new Random().Next(11).ToString()
                };

                foreach (var attachment in message.Tickets[0]?.Images)
                {
                    myEvent.Attachments.Add(new EventAttachment()
                    {
                        Title = attachment.Title,
                        FileUrl = attachment.Data
                    });
                }

                try
                {
                    myEvent = service.Events.Insert(myEvent, appSettings.calendarid).Execute();
                }
                catch (Exception ex)
                {
                    try
                    {
                        myEvent = service.Events.Update(myEvent, appSettings.calendarid, myEvent.Id).Execute();
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine(ex2.Message);
                    }
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