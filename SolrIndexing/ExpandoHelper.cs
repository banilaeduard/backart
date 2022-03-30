using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SolrIndexing
{
    public static class ExpandoHelper
    {
        public static T ToObject<T>(this IDictionary<string, object> source)
        where T : class, new()
        {
            if (typeof(T).Name != Convert.ToString(source[SolrConstants.MetaType]))
                throw new InvalidCastException("No meta type");

            var someObject = new T();
            var someObjectType = someObject.GetType();

            foreach (var item in source)
            {
                if (someObjectType.GetProperty(item.Key) != null)
                    someObjectType
                             .GetProperty(item.Key)
                             .SetValue(someObject, item.Value, null);
            }

            return someObject;
        }

        public static IDictionary<string, object> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
        {
            var dictionary = source.GetType().GetProperties(bindingAttr).toAgregateDictionary
            (
                propInfo => propInfo.Name,
                propInfo => propInfo.GetValue(source, null)
            );
            dictionary.Add(SolrConstants.MetaType, source.GetType().Name);

            return dictionary;
        }

        public static IDictionary<string, object> toAgregateDictionary<T>(this IEnumerable<T> objs, Func<T, string> keySelector, Func<T, object> valueSelector)
        {
            var result = new Dictionary<string, object>();

            foreach (var obj in objs)
                mergeToList(result, keySelector(obj).ToLower(), valueSelector(obj));

            return result;
        }

        public static IDictionary<string, object> mergeWith(this IDictionary<string, object> src, IDictionary<string, object> other)
        {
            if (other == null) return src;

            foreach (var kvp in other)
                mergeToList(src, kvp.Key, kvp.Value);

            return src;
        }

        public static IDictionary<string, object> mergeWith<T>(this IDictionary<string, object> src, KeyValuePair<string, T> kvp)
        {
            mergeToList(src, kvp.Key, kvp.Value);
            return src;
        }

        public static object unwrap(this IDictionary<string, object> src, string key)
        {
            object value = src[key];
            if (Convert.GetTypeCode(value) != TypeCode.Object)
            {
                return value;
            }
            else if (typeof(IList).IsAssignableFrom(value.GetType())
                        && ((IList)value).Count > 0)
            {
                return ((IList)value)[0];
            }
            return null;
        }

        private static void mergeToList(IDictionary<string, object> agregator, string key, object value)
        {
            if (value != null)
            {
                if (Convert.GetTypeCode(value) != TypeCode.Object)
                {
                    value = ValueConvertor.tryConvert(key, value);
                    if (agregator.ContainsKey(key))
                    {
                        if ((agregator[key] as IList<object>) == null)
                        {
                            var item = agregator[key];
                            agregator[key] = new List<object>();
                            ((IList<object>)agregator[key]).Add(item);
                        }
                        if (!((IList<object>)agregator[key]).Contains(value))
                            ((IList<object>)agregator[key]).Add(value);
                    }
                    else
                    {
                        agregator.Add(key, value);
                    }
                }
                else if (value.GetType().IsGenericType)
                {
                    var type = value.GetType().GetGenericTypeDefinition();
                    if (typeof(IDictionary).IsAssignableFrom(type)
                        && ((IDictionary)value).Count > 0)
                    {
                        var K = type.GetGenericArguments()[0];
                        var V = type.GetGenericArguments()[1];
                        if (K == typeof(string) && V == typeof(object))
                            agregator.Add(key, new Dictionary<string, object>().mergeWith((IDictionary<string, object>)value));
                    }
                    else if (typeof(IList).IsAssignableFrom(type)
                        && ((IList)value).Count > 0)
                    {
                        var list = (IList)value;
                        for (int i = 0; i < list.Count; i++)
                        {
                            mergeToList(agregator, key, list[i]);
                        }
                    }
                }
            }
        }
    }
}
