using Microsoft.EntityFrameworkCore;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using NER;
using DataAccess;
using DataAccess.Entities;
using DataAccess.Context;
using Storage;
using MimeKit;


namespace CronJob
{
    internal class EmailProcessor : IProcessor<MimeMessage>
    {
        ComplaintSeriesDbContext complaintSeriesDbContext;
        AppIdentityDbContext usersDbContext;
        IStorageService storageService;
        SentenceTokenizer sentTok;
        WordTokenizer wordTok;
        NameFinder nameFinder;
        HtmlStripper htmlStrip;
        private static Regex regexHtml = new Regex(@"(<br />|<br/>|</ br>|</br>)|<br>");
        private static Regex regexText = new Regex(@"\r\n|\n");

        public EmailProcessor(
            DbContextOptions<ComplaintSeriesDbContext> ctxBuilder,
            NoFilterBaseContext noFilter,
            AppIdentityDbContext usersDbContext,
            IStorageService storageService,
            SentenceTokenizer sentTok,
            WordTokenizer wordTok,
            NameFinder nameFinder,
            HtmlStripper htmlStrip)
        {
            complaintSeriesDbContext = new ComplaintSeriesDbContext(ctxBuilder, noFilter);
            this.usersDbContext = usersDbContext;
            this.storageService = storageService;
            this.sentTok = sentTok;
            this.wordTok = wordTok;
            this.nameFinder = nameFinder;
            this.htmlStrip = htmlStrip;
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

                var description = string.Empty;
                var body = !string.IsNullOrWhiteSpace(message.HtmlBody) ?
                    htmlStrip.StripHtml(regexHtml.Replace(message.HtmlBody, "\r\n")) : message.TextBody ?? "";
                var nrComanda = string.Empty;

                var sentences = sentTok.DetectSentences(body);
                description = sentences.Aggregate((agg, val) => agg = (agg ?? "") + " \r\n" + regexText.Replace(val, " "));

                var names = nameFinder.getNames(wordTok.Tokenize(body));

                foreach (var name in names)
                    if (name.Probability > 0.49999)
                        nrComanda += string.Format("{0}: {1}; ", name.Type, name.Value);

                var complaint = new ComplaintSeries()
                {
                    DataKey = dataKeyLocation,
                    DataKeyId = dataKeyLocation?.Id,
                    TenantId = "cubik",
                    NrComanda = nrComanda,
                    Tickets = new List<Ticket>() {
                        new Ticket() {
                            CodeValue = id,
                            Description = description,
                            Attachments = new List<Attachment>(),
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

                        string filePath;
                        using (var stream = storageService.TryAquireStream(message.MessageId, fileName, out filePath))
                        {
                            if (attachment is MessagePart)
                            {
                                var part = (MessagePart)attachment;
                                await part.Message.WriteToAsync(stream);
                            }
                            else
                            {
                                var part = (MimePart)attachment;
                                await part.Content.DecodeToAsync(stream);
                            }
                            var img = new Attachment()
                            {
                                Title = fileName,
                                Data = filePath,
                                Ticket = ticket,
                                CreatedDate = new DateTime(message.Date.Ticks),
                                UpdatedDate = new DateTime(message.ResentDate.Ticks),
                                ContentType = attachment.ContentType.MimeType,
                                Extension = Path.GetExtension(fileName),
                                StorageType = storageService.StorageType
                            };
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