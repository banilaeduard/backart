using Azure;
using Azure.Data.Tables;
using EntityDto.Tickets;

namespace RepositoryContract.Tickets
{
    public class TicketEntity : Ticket, ITableEntity
    {
        public ETag ETag { get; set; }
        public override string ToString()
        {
            return $@"{this.PartitionKey} - {this.RowKey}";
        }
    }
}
