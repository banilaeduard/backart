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
        public string gcalendarkey { get; set; }
        public string gcalendaruser { get; set; }
        public string calendarid { get; set; }
        public string calreccurencepattern { get; set; }
        public string mailreccurencepattern { get; set; }
    }
}