namespace AzureServices
{
    public static class AzureTableKeySanitizer
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
    }

}
