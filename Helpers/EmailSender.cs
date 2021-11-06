using System.Threading.Tasks;

using SendGrid;
using SendGrid.Helpers.Mail;

namespace WebApi.Helpers
{
    public class EmailSender
    {
        string key;
        string fromEmail;
        string fromName;
        public EmailSender(AppSettings settings)
        {
            key = settings.SendGridKey;
            fromEmail = settings.SendGridFromEmail;
            fromName = settings.SendGridName;
        }
        public void SendEmail(string userEmail, string confirmationLink)
        {
            var client = new SendGridClient(key);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(fromEmail, fromName),
                Subject = "Confirm your email",
                HtmlContent = confirmationLink
            };

            msg.AddTo(new EmailAddress(userEmail));
            msg.SetClickTracking(false, false);
            client.SendEmailAsync(msg).Forget();
        }
    }
}