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
            var temp = new Dictionary<string, object>(document);

            if (temp.ContainsKey("id"))
            {
                var oldObjs = await query(0, 1, string.Format("id:{0}", Convert.ToString(temp["id"])));
                if (oldObjs?.Count > 0)
                {
                    var oldDoc = oldObjs[0];
                    foreach (var kvp in oldDoc)
                    {
                        if (!temp.ContainsKey(kvp.Key))
                            temp.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            foreach (var key in SolrConstants.ignoreFields)
            {
                if (temp.ContainsKey(key))
                {
                    temp.Remove(key);
                }
            }

            await client.AddAsync(temp);
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
            AbstractSolrQuery query = new SolrQuery(querys[0]);
            foreach (var param in querys.Skip(1))
            {
                query += new SolrQuery(param);
            }
            return await client.QueryAsync(query, new QueryOptions()
            {
                StartOrCursor = new StartOrCursor.Start(start),
                Rows = rows
            });
        }
    }
}
