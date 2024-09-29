using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace RepositoryContract.Tickets
{
    public class AttachmentEntry : ITableEntity
    {
        [Key]
        public int Id { get; set; }
        public string Data { get; set; }
        public string Title { get; set; }
        public string ContentType { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string RefPartition { get; set; }
        public string RefKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
