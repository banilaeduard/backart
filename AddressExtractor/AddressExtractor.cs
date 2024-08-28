using System.Fabric;
using System.Text;
using System.Text.RegularExpressions;
using Entities.Remoting;
using Entities.Remoting.Jobs;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using NER;

namespace AddressExtractor
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class AddressExtractor : StatelessService, IAddressExtractor
    {
        private static Regex nrComdanda = new Regex(@"4\d{9}");
        private static readonly string[] punctuation = { ".", ":", "!", ",", "-" };
        private static readonly string[] address = { "jud", "judet", "com", "comuna", "municipiul", "mun",
                                                                    "str", "strada", "oras", "soseaua", "valea",
                                                                    "sat", "satu", "cod postal", "postal code",
                                                                    "bulevardul", "bulevard", "bdul", "bld-ul", "b-dul",
                                                                    "calea", "aleea", "sos", "sect", "sectorul", "sector" };

        public AddressExtractor(StatelessServiceContext context)
            : base(context)
        { }

        Task<Extras> IAddressExtractor.Parse(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return Task.FromResult(new Extras());

            SentenceTokenizer sentTok = new();
            WordTokenizer wordTok = new();
            HashSet<string> addressEntry = new();
            string nrComanda = String.Empty;
            StringBuilder bodyResult = new();

            try
            {
                var addressLine = string.Empty;

                bool shouldProcess = false;
                bool ignoreNextPunctaion = false;
                bool nearAddress = false;
                bool postalCode = true;
                bool wasCountry = false;
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
                        }
                    }
                    
                    var words = wordTok.Tokenize(sent);

                    foreach (var item in words)
                    {
                        bodyResult.Append(item + " ");
                    }
                    bodyResult.AppendLine();

                    var descLine = string.Empty;
                    for (int i = 0; i < words.Length; i++)
                    {
                        var word = words[i];
                        nearAddress = lastAddressIndex != -1
                            && (i - lastAddressIndex + offsetAddressIndex) < 3;

                        if (!nearAddress
                            || string.Equals(word, "tel", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                              || string.Equals(word, "telefon", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                                || string.Equals(word, "telephone", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                                    || string.Equals(word, "phone", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                              )
                        {
                            if (!string.IsNullOrWhiteSpace(addressLine) &&
                                !addressEntry.Contains(addressLine.Trim()) && addressHits > 1)
                            {
                                addressEntry.Add(addressLine.Trim().Trim(punctuation.Select(t => t[0]).ToArray()));
                            }
                            addressLine = string.Empty;
                            shouldProcess = false;
                            ignoreNextPunctaion = false;
                            postalCode = true;
                            wasCountry = false;
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
                                addressHits++;
                            }
                        }
                        else if (shouldProcess)
                        {
                            if (Char.IsLetterOrDigit(word[0]))
                            {
                                addressLine += word + " ";
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

                    offsetAddressIndex += words.Length - 1;
                }

                if (!string.IsNullOrWhiteSpace(addressLine) &&
                                !addressEntry.Contains(addressLine.Trim()) && addressHits > 1)
                    addressEntry.Add(addressLine.Trim());
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "Error-{0}", ex.Message);
            }
            return Task.FromResult(new Extras()
            {
                Addreses = addressEntry.ToArray(),
                NumarComanda = nrComanda,
                BodyResult = bodyResult.ToString().Trim(),
            });
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return [
                new ServiceInstanceListener(
                    (context) => new FabricTransportServiceRemotingListener(context, this))
                ];
        }
    }
}
