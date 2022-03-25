using SolrNet;
using SolrNet.Commands.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SolrIndexing
{
    public class SolrCRUD
    {
        ISolrOperations<Dictionary<string, object>> client;
        public SolrCRUD(ISolrOperations<Dictionary<string, object>> svc)
        {
            client = svc;
        }
        public async Task createDocument(IDictionary<string, object> document)
        {
            var tst = new Dictionary<string, object>(document);
            foreach (var key in SolrConstants.ignoreFields)
            {
                if (tst.ContainsKey(key))
                {
                    tst.Remove(key);
                }
            }
            await client.AddAsync(tst);
            await client.CommitAsync();
        }

        public async Task updateDocument(IDictionary<string, object> document)
        {
            await deleteDocument(Convert.ToString(document["id"]));
            await createDocument(document);
        }

        public async Task deleteDocument(string id)
        {
            // not working atm
            await client.DeleteAsync(id);
            await client.CommitAsync();
        }

        public async Task<SolrQueryResults<Dictionary<string, object>>> query(int start, int rows, params string[] querys)
        {
            var query = querys[0];
            foreach (var param in querys.Skip(1))
            {
                query += new SolrQuery(param);
            }
            return await client.QueryAsync(query, new QueryOptions()
            {
                StartOrCursor = new StartOrCursor.Start(start),
                Rows = rows,
                Highlight = new HighlightingParameters()
                {
                    MergeContiguous = true,
                }
            });
        }
    }
}
