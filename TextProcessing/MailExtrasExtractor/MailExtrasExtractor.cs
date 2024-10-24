using System.Fabric;
using AddressExtractorImpl;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using NumarComandaExtractor;
using Tokenizer;

namespace MailExtrasExtractor
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class MailExtrasExtractor : StatelessService, IMailExtrasExtractor
    {
        private static readonly AddressExtractorService addrExtractor = new();
        private static readonly NumarComandaExtractorService nrComandaExtractor = new();
        private static readonly TokenizerService tokService = new();
        public MailExtrasExtractor(StatelessServiceContext context)
            : base(context)
        { }

        async Task<Extras> IMailExtrasExtractor.Parse(string body)
        {
            var stripHtmp = await tokService.HtmlStrip(body.Replace("<br/>", ""));
            var sentences = await tokService.GetSentences(stripHtmp);
            var newBody = string.Join(Environment.NewLine, sentences);

            if (string.IsNullOrWhiteSpace(body))
            {
                return new Extras() { BodyResult = newBody };
            }

            return new Extras()
            {
                Addreses = await addrExtractor.Parse(sentences),
                NumarComanda = await nrComandaExtractor.Extract(newBody),
                BodyResult = newBody,
            };
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            //ServiceEventSource.Current.ServiceMessage(this.Context, "Service name is {0}. Listen address is {1}", Context.ServiceName.ToString(), Context.ListenAddress);
            return this.CreateServiceRemotingInstanceListeners();
        }
    }
}