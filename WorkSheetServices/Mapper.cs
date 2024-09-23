using ClosedXML.Excel;
using System.Reflection;
using EntityDto;

namespace WorkSheetServices
{
    internal class Mapper<T> where T : class, new()
    {
        IDictionary<MapExcelAttribute, PropertyInfo> _internalMap = new Dictionary<MapExcelAttribute, PropertyInfo>();
        public Mapper()
        {
            Type t = typeof(T);
            foreach (PropertyInfo p in t.GetProperties())
            {
                object[] attributes = p.GetCustomAttributes(typeof(MapExcelAttribute), true);
                if (attributes?.Length > 0)
                {
                    var attr = (MapExcelAttribute)attributes[0];
                    if (attr.type == null)
                    {
                        attr.type = p.PropertyType;
                    }
                    _internalMap.Add(attr, p);
                }
            }
        }

        public static Type ChangeType(Type conversion)
        {
            if (conversion.IsGenericType && conversion.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                conversion = Nullable.GetUnderlyingType(conversion)!;
            }

            return conversion;
        }

        public T ReadLines(Func<int, int, IXLCell> getValue, int rowIndex)
        {
            var item = new T();

            foreach (var kvp in _internalMap)
            {
                var (k, v) = kvp;
                v.SetValue(item, CastTo(kvp, getValue(k.GetColNumber(), k.GetRowNumber() ?? rowIndex)));
            }

            return item;
        }

        private object CastTo(KeyValuePair<MapExcelAttribute, PropertyInfo> fieldInfo, IXLCell val)
        {
            var sourceType = fieldInfo.Key.GetParseFrom();
            var targetType = ChangeType(fieldInfo.Value.PropertyType);
            if (val.IsEmpty()
                && fieldInfo.Value.PropertyType.IsGenericType 
                && fieldInfo.Value.PropertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) return null;

            if (sourceType.IsAssignableFrom(typeof(int)))
            {
                return Convert.ChangeType(val.Value.GetNumber(), targetType);
            }

            if (sourceType.IsAssignableFrom(typeof(DateTime)))
            {
                return Convert.ChangeType(val.Value.GetDateTime(), targetType);
            }

            if (sourceType.IsAssignableFrom(typeof(long)))
            {
                return Convert.ChangeType(val.Value.GetNumber(), targetType);
            }

            if (sourceType.IsAssignableFrom(typeof(string)))
            {
                return (val.Value.IsBlank || val.IsEmpty()) ? "" : Convert.ChangeType(val.Value.GetText(), targetType);
            }

            throw new NotImplementedException(string.Format("{0} -> {1} missing", sourceType, targetType));
        }
    }
}
