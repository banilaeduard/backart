namespace EntityDto
{
    public abstract class TrackableTableEntity : ITableEntryDto
    {
        private readonly Dictionary<string, object?> _values = new();

        protected void Set<T>(string name, T value)
        {
            _values[name] = value;
        }

        protected T Get<T>(string name)
        {
            return _values.TryGetValue(name, out var val) ? (T)val! : default!;
        }

        public IReadOnlyDictionary<string, object?> ChangedProperties => _values;

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public int Id { get; set; }
    }
}
