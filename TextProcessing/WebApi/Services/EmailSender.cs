namespace WebApi.Services
{
    using SendGrid;
    using SendGrid.Helpers.Mail;
    using Microsoft.Extensions.Logging;
    using System.Fabric;

    public class EmailSender
    {
        ILogger<EmailSender> logger;
        string key;
        string fromEmail;
        string fromName;
        public EmailSender(StatelessServiceContext settings, ILogger<EmailSender> logger)
        {
            key = Environment.GetEnvironmentVariable("SendGridKey")!;
            fromEmail = "banila.eduard@gmail.com";
            fromName = "Adrian B.";
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
            msg.SetClickTracking(true, false);
            client.SendEmailAsync(msg);
        }
    }
}