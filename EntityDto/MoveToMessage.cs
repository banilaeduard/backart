namespace EntityDto
{
    public class MoveToMessage<T>
    {
        public required string DestinationFolder { get; set; }
        public required IEnumerable<T> Items { get; set; }
    }
}
