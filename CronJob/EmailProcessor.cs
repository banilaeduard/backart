﻿using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using DataAccess;
using DataAccess.Entities;
using DataAccess.Context;
using Storage;
using System.IO;
using MimeKit;
using System.Text.RegularExpressions;

namespace CronJob
{
    internal class EmailProcessor : IProcessor<MimeMessage>
    {
        ComplaintSeriesDbContext complaintSeriesDbContext;
        AppIdentityDbContext usersDbContext;
        IStorageService storageService;
        Regex regex = new Regex(@"(<br />|<br/>|</ br>|</br>)|<br>");
        public EmailProcessor(
            DbContextOptions<ComplaintSeriesDbContext> ctxBuilder,
            NoFilterBaseContext noFilter,
            AppIdentityDbContext usersDbContext,
            IStorageService storageService)
        {
            complaintSeriesDbContext = new ComplaintSeriesDbContext(ctxBuilder, noFilter);
            this.usersDbContext = usersDbContext;
            this.storageService = storageService;
        }
        public async Task process(MimeMessage message, string id)
        {
            try
            {
                var email = message.From.Mailboxes?.ToList()[0]?.Address;
                if (email == null) return;

                var dataKeyLocation = usersDbContext.DataKeyLocation
                    .Where(t => t.name == email)
                    .SingleOrDefault();

                if (dataKeyLocation == null)
                {
                    dataKeyLocation = new DataKeyLocation()
                    {
                        name = email,
                        locationCode = email
                    };
                }

                var composed_id = string.Format("{0}_{1}", id, string.IsNullOrWhiteSpace(message.HtmlBody) ? "text" : "html");
                var complaint = new ComplaintSeries()
                {
                    DataKey = string.IsNullOrEmpty(dataKeyLocation.Id) ? dataKeyLocation : null,
                    DataKeyId = dataKeyLocation.Id,
                    TenantId = "cubik",
                    Tickets = new List<Ticket>() {
                        new Ticket() {
                            CodeValue = composed_id,
                            Description = string.IsNullOrEmpty(message.HtmlBody) ? message.TextBody : regex.Replace(message.HtmlBody, ""),
                            Images = new List<Image>(),
                            CreatedDate = new DateTime(message.Date.Ticks),
                            UpdatedDate = new DateTime(message.ResentDate.Ticks),
                        }
                    },
                    CreatedDate = new DateTime(message.Date.Ticks),
                    UpdatedDate = new DateTime(message.ResentDate.Ticks),
                };

                if (message.Attachments?.Count() > 0)
                {
                    var ticket = complaint.Tickets[0];

                    foreach (var attachment in message.Attachments)
                    {
                        var fileName = attachment.ContentDisposition?.FileName ?? attachment.ContentType.Name;

                        using (var stream = new MemoryStream())
                        {
                          if (attachment is MessagePart) {
                                var part = (MessagePart) attachment;

                                part.Message.WriteTo (stream);
                            } else {
                                var part = (MimePart) attachment;

                                part.Content.DecodeTo (stream);
                            }

                            var img = new Image()
                            {
                                Title = fileName
                            }; await attachment.WriteToAsync(stream);

                            img.Data = storageService.Save(stream.ToArray(), fileName);
                            img.Ticket = ticket;
                            this.complaintSeriesDbContext.Entry(img).State = EntityState.Added;
                        }
                    }
                }

                complaintSeriesDbContext.Complaints.Add(complaint);
                await complaintSeriesDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task<bool> shouldProcess(MimeMessage _, string id)
        {
            return await Task.FromResult(
                complaintSeriesDbContext.Ticket
                                .Where(t => t.CodeValue.StartsWith(id)).Count() == 0
                );
        }
    }
}