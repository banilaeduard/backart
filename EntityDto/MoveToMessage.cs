namespace EntityDto
{
    public class MoveToMessage<T>
    {
        public MoveToMessage() { }
        public int TransportId { get; set; }
        public string DestinationFolder { get; set; }
        public IEnumerable<T> Items { get; set; }
    }
}
