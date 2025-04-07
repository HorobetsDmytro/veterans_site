using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using veterans_site.Models;

namespace veterans_site.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<EmailSettings> emailSettings, ILogger<EmailSender> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                message.To.Add(new MailAddress(email));

                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
                {
                    client.EnableSsl = _emailSettings.EnableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);

                    await client.SendMailAsync(message);
                    _logger.LogInformation($"Email sent successfully to {email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email: {ex.Message}");
                throw;
            }
        }
    }
}