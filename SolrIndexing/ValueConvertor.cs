using System;

namespace SolrIndexing
{
    internal class ValueConvertor
    {
        public static object tryConvert(string key, object value)
        {
            try
            {
                if (SolrConstants.Fields.TryGetValue(key, out var fieldType))
                {
                    switch (fieldType)
                    {
                        case "double": return Convert.ToDouble(value);
                        case "int": return Convert.ToInt64(value);
                        case "date": return Convert.ToDateTime(value);
                    }
                }
                return Convert.ToString(value)?.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} - {1} - {2}", key, value, ex.Message);
            }

            return "";
        }
    }
}
