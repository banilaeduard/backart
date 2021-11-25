namespace WebApi.Helpers
{
    using SendGrid;
    using SendGrid.Helpers.Mail;
    using Microsoft.Extensions.Logging;

    public class EmailSender
    {
        ILogger<EmailSender> logger;
        string key;
        string fromEmail;
        string fromName;
        public EmailSender(AppSettings settings, ILogger<EmailSender> logger)
        {
            key = settings.SendGridKey;
            fromEmail = settings.SendGridFromEmail;
            fromName = settings.SendGridName;
            this.logger = logger;
        }
        public void SendEmail(string userEmail, string confirmationLink, string subject)
        {
            var client = new SendGridClient(key);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(fromEmail, fromName),
                Subject = subject,
                HtmlContent = confirmationLink
            };

            msg.AddTo(new EmailAddress(userEmail));
            msg.SetClickTracking(false, false);
            client.SendEmailAsync(msg).Forget(this.logger);
        }
    }
}