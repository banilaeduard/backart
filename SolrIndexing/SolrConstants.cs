using System.Collections.Generic;

namespace SolrIndexing
{
    public class SolrConstants
    {
        public const string MetaType = "meta_type";
        public const string SourceField = "source_from";
        public static readonly List<string> ignoreFields = new List<string>() { "_version_" };
    }
}
