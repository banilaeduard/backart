using Azure.Data.Tables;

namespace AzureServices
{
    public static class BatchHelper
    {
        //public static (IEnumerable<TableTransactionAction> items, TableStorageService self)
        //    Concat(
        //            this (IEnumerable<TableTransactionAction> items, TableStorageService self) left,
        //            (IEnumerable<TableTransactionAction> items, TableStorageService self) right
        //    ) => (Enumerable.Concat(left.items, right.items), left.self);

        public static (List<TableTransactionAction> items, TableStorageService self)
            Concat(
                    this (List<TableTransactionAction> items, TableStorageService self) left,
                    (List<TableTransactionAction> items, TableStorageService self) right
            )
        {
            left.items.AddRange(right.items);
            return left;
        }

        public static async Task ExecuteBatch(this (IEnumerable<TableTransactionAction> items, TableStorageService self) trans, string? tableName = null)
          => await trans.self.ExecuteBatch(trans.items, tableName);
    }
}