using DataAccess.Entities;
using NER;
using SolrIndexing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Piping
{
    public class EnrichService
    {
        NameFinder nameFinder;
        WordTokenizer wordTokenizer;
        SolrCRUD solrIndex;
        public EnrichService(
            NameFinder nameFinder,
            WordTokenizer wordTokenizer,
            SolrCRUD solrIndex)
        {
            this.nameFinder = nameFinder;
            this.wordTokenizer = wordTokenizer;
            this.solrIndex = solrIndex;
        }
        public async Task Enrich(
            Ticket model,
            ComplaintSeries series,
            Source src = Source.UserInput,
            IDictionary<string, object> extras = null)
        {
            try
            {
                var dict = model.AsDictionary().mergeWith(extras);
                dict[SolrConstants.SourceField] = src;

                if (series.DataKey != null)
                {
                    dict["datakey"] = new List<string>() { series.DataKey.locationCode, series.DataKey.name };
                }

                var tokens = wordTokenizer.Tokenize(model.Description);
/*                var names = nameFinder.getNames(tokens);

                dict.mergeWith(names.toAgregateDictionary(t => t.Type, t => t.Value));*/

                await solrIndex.createDocument(dict);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

