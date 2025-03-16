using Azure;
using Azure.Data.Tables;
using EntityDto;
using EntityDto.Tickets;
using System.Diagnostics.CodeAnalysis;

namespace RepositoryContract.Tickets
{
    public class AttachmentEntry : Attachment ,ITableEntity, ITableEntryDto<AttachmentEntry>
    {
        public ETag ETag { get; set; }

        public bool Equals(AttachmentEntry? x, AttachmentEntry? y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] AttachmentEntry obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
