using Azure;
using Azure.Data.Tables;
using EntityDto;
using EntityDto.Tickets;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.Tickets
{
    public class TicketEntity : Ticket, ITableEntity, ITableEntryDto<TicketEntity>
    {
        public ETag ETag { get; set; }

        public bool Equals(TicketEntity? x, TicketEntity? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] TicketEntity obj)
        {
            return base.GetHashCode(obj);
        }
    }
}