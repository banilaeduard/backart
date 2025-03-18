namespace SqlTableRepository.ProductCodes
{
    static internal class ProductCodesSql
    {
        internal static string GetProductCodes() => $@"with dif as (
                                                    select p.Code as RootCode, p.Code as ParentCode,1 as [Level], p.* from dbo.ProductCodes p WHERE InternalParentId is null
                                                    UNION ALL
                                                    select dif.RootCode, dif.Code as ParentCode, dif.[Level] + 1 , p.* from dbo.ProductCodes p
                                                    join dif on p.InternalParentId = dif.InternalId
                                                    ) select * from dif";
        internal static string GetProductCodeStats() => $@"SELECT * FROM dbo.ProductCodeStats";
        internal static string GetProductStats() => $@"SELECT * FROM dbo.ProductStats";
    }
}
