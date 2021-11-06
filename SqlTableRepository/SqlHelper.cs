using Dapper;
using System.Linq.Expressions;
using System.Text;

namespace SqlTableRepository
{
    internal static class SqlHelper
    {
        static SqlHelper()
        {
            //        SqlMapper.SetTypeMap(
            //typeof(TModel),
            //new CustomPropertyTypeMap(
            //    typeof(TModel),
            //    (type, columnName) =>
            //        type.GetProperties().FirstOrDefault(prop =>
            //            prop.GetCustomAttributes(false)
            //                .OfType<ColumnAttribute>()
            //                .Any(attr => attr.Name == columnName))));
        }

        /// <summary>
        /// (VALUES (x0,y0), (x1,y1)) as {sqlName}({x,y})
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range"></param>
        /// <param name="dParams"></param>
        /// <param name="sqlName"></param>
        /// <param name="selectors"></param>
        /// <returns></returns>
        internal static string FromValues<T>(this IEnumerable<T> range, DynamicParameters dParams, string sqlName, params Expression<Func<T, object>>[] selectors)
        {
            Dictionary<string, Func<T, object>> selectorNames = selectors.ToDictionary(selector => GetExpressionName(selector), t => t.Compile());
            var itemCount = range.Count();

            StringBuilder sql = new("(VALUES");
            for (int i = 0; i < itemCount; i++)
            {
                sql.AppendLine("(");
                for (int j = 0; j < selectorNames.Count; j++)
                {
                    var kvp = selectorNames.ElementAt(j);

                    dParams.Add($"@{kvp.Key}{i}", kvp.Value(range.ElementAt(i)));

                    sql.Append($"@{kvp.Key}{i}{(j == selectorNames.Count - 1 ? "" : ", ")}");
                }
                sql.AppendLine($"){(i == itemCount - 1 ? "" : ",")}");
            }
            sql.AppendLine($") as {sqlName}({string.Join(", ", selectorNames.Select(t => t.Key))})");
            return sql.ToString();
        }

        private static string GetExpressionName<T>(Expression<Func<T, object>> selector)
        {
            var body = selector.Body as MemberExpression;
            if (body == null)
            {
                body = ((UnaryExpression)selector.Body).Operand as MemberExpression;
            }
            return body.Member.Name;
        }
    }
}
