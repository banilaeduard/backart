using Azure.Data.Tables;
using EntityDto;

namespace AzureServices
{
    public static class AzureTableUtils
    {
        private static readonly char[] InvalidChars = { '/', '\\', '#', '?' };

        public static string Sanitize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("PartitionKey or RowKey cannot be null or empty.");

            var sanitized = new string(input
                .Where(c => !InvalidChars.Contains(c))
                .ToArray());

            // Truncate if needed (optional)
            if (sanitized.Length > 1024)
                sanitized = sanitized.Substring(0, 1024);

            return sanitized;
        }

        public static TableEntity ToTableEntityFromTracked(this TrackableTableEntity tracked)
        {
            var entity = new TableEntity(tracked.PartitionKey, tracked.RowKey);

            foreach (var kvp in tracked.ChangedProperties)
            {
                entity[kvp.Key] = kvp.Value;
            }

            return entity;
        }
    }
}
