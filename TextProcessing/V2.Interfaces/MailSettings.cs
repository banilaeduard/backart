﻿namespace YahooFeeder
{
    public class MailSettings
    {
        public required string User { get; set; }
        public required string Password { get; set; }
        public required int DaysBefore { get; set; }
    }
}
