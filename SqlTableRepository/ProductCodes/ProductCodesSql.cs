namespace SqlTableRepository.ProductCodes
{
    static internal class ProductCodesSql
    {
        internal static string GetProductCodes() => $@"dbo.GetProductCodes";
        internal static string GetProductCodeStats() => $@"SELECT * FROM dbo.ProductCodeStats";
        internal static string GetProductStats() => $@"SELECT * FROM dbo.ProductStats";
    }
}
