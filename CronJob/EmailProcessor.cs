using Microsoft.EntityFrameworkCore;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using NER;
using Piping;
using SolrIndexing;
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
        HtmlStripper htmlStrip;
        EnrichService enrichService;

        private static Regex regexHtml = new Regex(@"(<br />|<br/>|</ br>|</br>)|<br>");
        private static Regex regexNewLine = new Regex(@"\r\n|\r|\n");
        private static Regex nrComdanda = new Regex(@"4\d{9}");
        private static readonly string[] punctuation = new string[] { ".", ":", "!", ",", "-" };
        private static readonly string[] address = new string[] { "jud", "judet", "com", "comuna", "municipiul", "mun",
                                                                    "str", "strada", "oras", "soseaua", "valea",
                                                                    "sat", "satu", "cod postal", "postal code",
                                                                    "bulevardul", "bulevard", "bdul", "bld-ul", "b-dul",
                                                                    "calea", "aleea", "sos", "sect", "sectorul", "sector" };
        public EmailProcessor(
            DbContextOptions<ComplaintSeriesDbContext> ctxBuilder,
            NoFilterBaseContext noFilter,
            AppIdentityDbContext usersDbContext,
            IStorageService storageService,
            SentenceTokenizer sentTok,
            WordTokenizer wordTok,
            HtmlStripper htmlStrip,
            EnrichService enrichService)
        {
            complaintSeriesDbContext = new ComplaintSeriesDbContext(ctxBuilder, noFilter);
            this.usersDbContext = usersDbContext;
            this.storageService = storageService;
            this.sentTok = sentTok;
            this.wordTok = wordTok;
            this.htmlStrip = htmlStrip;
            this.enrichService = enrichService;
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

                var body = !string.IsNullOrWhiteSpace(message.HtmlBody) ?
                    htmlStrip.StripHtml(regexHtml.Replace(message.HtmlBody, " ")) : message.TextBody ?? "";

                if (string.IsNullOrWhiteSpace(body)) return;

                var nrComanda = string.Empty;
                var status = string.Empty;
                var description = string.Empty;
                var extras = new Dictionary<string, object>();

                var addressLine = string.Empty;
                var addressEntry = new HashSet<string>();
                bool shouldProcess = false;
                bool ignoreNextPunctaion = false;
                bool nearAddress = false;
                bool postalCode = true;
                bool wasCountry = false;
                // bool? wasDigit = null;
                int wasNumber = 0;
                int number = 0;
                int addressHits = 0;
                int lastAddressIndex = -1;
                int offsetAddressIndex = 0;

                foreach (var sent in sentTok.DetectSentences(body))
                {
                    var match = nrComdanda.Match(sent);
                    if (match.Success)
                    {
                        for (int i = 0; i < match.Groups.Count; i++)
                        {
                            nrComanda += match.Groups[i].Value + " ";
                            double numarComanda = 0;
                            if (double.TryParse(match.Groups[i].Value, out numarComanda))
                            {
                                extras.mergeWith(KeyValuePair.Create("comanda", numarComanda));
                            }
                        }
                    }
                    var words = wordTok.Tokenize(sent);

                    var descLine = string.Empty;
                    for (int i = 0; i < words.Length; i++)
                    {
                        var word = words[i];
                        nearAddress = lastAddressIndex != -1
                            && (i - lastAddressIndex + offsetAddressIndex) < 3;

                        if (!nearAddress)
                        {
                            if (!string.IsNullOrWhiteSpace(addressLine) &&
                                !addressEntry.Contains(addressLine.Trim()) && addressHits > 1)
                            {
                                status += " " + addressLine.Trim().Trim(punctuation.Select(t => t[0]).ToArray()) + " / ";
                                addressEntry.Add(addressLine.Trim().Trim(punctuation.Select(t => t[0]).ToArray()));
                            }
                            addressLine = string.Empty;
                            shouldProcess = false;
                            ignoreNextPunctaion = false;
                            postalCode = true;
                            wasCountry = false;
                            // bool? wasDigit = null;
                            wasNumber = 0;
                            number = 0;
                            addressHits = 0;
                            lastAddressIndex = -1;
                        }

                        if (address.Contains(word, StringComparer.InvariantCultureIgnoreCase)
                            || (word.Contains("nr", StringComparison.InvariantCultureIgnoreCase) && nearAddress))
                        {
                            if (word.Contains("nr", StringComparison.InvariantCultureIgnoreCase))
                            {
                                wasNumber++;
                            }
                            if (wasNumber < 2)
                            {
                                addressLine += word + " ";
                                shouldProcess = true;
                                ignoreNextPunctaion = true;
                                lastAddressIndex = i + offsetAddressIndex;
                                postalCode = true;
                                //wasDigit = null;
                                addressHits++;
                            }
                        }
                        else if (shouldProcess
                            // && (!wasDigit.HasValue || wasDigit.Value == Char.IsDigit(word[0]))
                            )
                        {
                            if (Char.IsLetterOrDigit(word[0]))
                            {
                                addressLine += word + " ";
                                // wasDigit = Char.IsDigit(word[0]);
                            }
                            else if (ignoreNextPunctaion)
                                addressLine += word + " ";
                            else
                                shouldProcess = false;

                            lastAddressIndex = i + offsetAddressIndex;
                            ignoreNextPunctaion = false;
                        }
                        else if (nearAddress && !wasCountry && wasNumber < 2)
                        {
                            if (punctuation.Contains(word))
                            {
                                addressLine += word + " ";
                                lastAddressIndex++;
                            }
                            else if (int.TryParse(word, out number) && postalCode)
                            {
                                addressLine += number + " ";
                                extras.mergeWith(KeyValuePair.Create("postalcode", number));
                                postalCode = false;
                            }
                            else
                            {
                                if (new string[] { "RO", "Romania" }.Contains(word, StringComparer.InvariantCultureIgnoreCase))
                                {
                                    wasCountry = true;
                                }
                                addressLine += word + " ";
                            }
                        }
                        else
                        {
                            lastAddressIndex = -1;
                        }

                        descLine += (punctuation.Contains(word) ? "" : " ") + word;
                    }

                    description += descLine.Trim() + " " + Environment.NewLine;
                    offsetAddressIndex += words.Length - 1;
                }

                if (!string.IsNullOrWhiteSpace(addressLine) &&
                                !addressEntry.Contains(addressLine.Trim()) && addressHits > 1)
                {
                    status += " " + addressLine.Trim() + " / ";
                    addressEntry.Add(addressLine.Trim());
                }

                if (!string.IsNullOrEmpty(status))
                {
                    extras.mergeWith(addressEntry.toAgregateDictionary(t => "adresa", t => t));
                }

                var complaint = new ComplaintSeries()
                {
                    Status = status.Trim(),
                    DataKey = string.IsNullOrEmpty(dataKeyLocation.Id) ? dataKeyLocation : null,
                    DataKeyId = dataKeyLocation.Id,
                    TenantId = "cubik",
                    NrComanda = nrComanda,
                    Tickets = new List<Ticket>() {
                        new Ticket() {
                            CodeValue = string.Format("{0}", id), //!string.IsNullOrWhiteSpace(message.HtmlBody) ? "html" : "text"),
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
                            complaintSeriesDbContext.Entry(img).State = EntityState.Added;
                        }
                    }
                }

                complaintSeriesDbContext.Complaints.Add(complaint);
                await complaintSeriesDbContext.SaveChangesAsync();

                // indexing
                await enrichService.Enrich(complaint.Tickets[0], complaint, Source.MailImport, extras);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR EMAIL PROCESSOR");
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