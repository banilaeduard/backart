namespace EntityDto
{
    public class MoveToMessage<T>
    {
        public MoveToMessage() { }
        public string DestinationFolder { get; set; }
        public IEnumerable<T> Items { get; set; }
    }
}
