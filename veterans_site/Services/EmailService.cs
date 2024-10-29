using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using veterans_site.Models;
using MailKit.Net.Smtp;

namespace veterans_site.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendConsultationConfirmationAsync(
            string toEmail,
            string recipientName,
            string consultationTitle,
            DateTime consultationDateTime)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = "Підтвердження запису на консультацію";

                var builder = new BodyBuilder();
                builder.HtmlBody = $@"
                <html>
                <body>
                    <h2>Вітаю, {recipientName}!</h2>
                    <p>Ви успішно зареєструвалися на консультацію ""{consultationTitle}"".</p>
                    <p>Дата та час початку: {consultationDateTime:dd.MM.yyyy HH:mm}</p>
                    <p>Не запізнюйтесь 😊</p>
                    <br>
                    <p>З повагою,<br>Команда підтримки ветеранів</p>
                </body>
                </html>
                ";

                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailSettings.FromEmail, _emailSettings.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation($"Confirmation email sent to {toEmail} for consultation {consultationTitle}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email: {ex.Message}");
                throw;
            }
        }
    }
}
