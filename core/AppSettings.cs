namespace core
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string SendGridKey { get; set; }
        public string SendGridFromEmail { get; set; }
        public string SendGridName { get; set; }
        public string salt { get; set; }
        public string yapppass { get; set; }
        public string yappuser { get; set; }
    }
}