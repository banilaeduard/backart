using System.Collections.Generic;

namespace SolrIndexing
{
    public class SolrConstants
    {
        public const string MetaType = "meta_type";
        public const string SourceField = "source_from";
        public static readonly Dictionary<string, string> Fields = new Dictionary<string, string>()
        {
            { "comanda", "double" },
            { "createddate", "date" },
            { "updateddate", "date" },
        };
        public static readonly List<string> ignoreFields = new List<string>() { "_version_" };
    }
}
