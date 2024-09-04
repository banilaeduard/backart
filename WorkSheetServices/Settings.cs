namespace WorkSheetServices
{
    public class Settings<T> where T: class, new()
    {
        public Settings()
        {
            this.Mapper = new Mapper<T>();
        }
        public int FirstDataRow { get; set; }
        public int? LastDataRow { get; set; }
        internal Mapper<T> Mapper { get; private set; }
    }
}
