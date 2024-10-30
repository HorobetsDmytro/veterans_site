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

        public async Task SendRoleChangedEmailAsync(string toEmail, string userName, string newRole)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = "Зміна ролі в системі підтримки ветеранів";

                var builder = new BodyBuilder();
                builder.HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; margin: 0; padding: 20px;'>
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                        <h2 style='color: #2c3e50;'>Вітаємо, {userName}!</h2>
                        <p>Повідомляємо, що адміністратор змінив вашу роль у системі.</p>
                        <p>Ваша нова роль: <strong>{newRole}</strong></p>
                        <p>Якщо у вас виникли питання, будь ласка, зв'яжіться з адміністрацією.</p>
                        <br>
                        <p style='color: #7f8c8d;'>З повагою,<br>Команда підтримки ветеранів</p>
                    </div>
                </body>
                </html>";

                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailSettings.FromEmail, _emailSettings.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation($"Role change email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending role change email: {ex.Message}");
                throw;
            }
        }

        // Services/EmailService.cs
        public async Task SendRoleChangeConfirmationEmailAsync(string toEmail, string userName, string newRole, string confirmationLink, string rejectLink)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Запит на зміну ролі";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
        <html>
        <head>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                .container {{
                    padding: 20px;
                    font-family: Arial, sans-serif;
                    max-width: 600px;
                    margin: 0 auto;
                }}
                .button {{
                    display: inline-block;
                    padding: 15px 30px;
                    margin: 10px;
                    text-decoration: none;
                    border-radius: 5px;
                    font-weight: bold;
                    color: white !important;
                }}
                @media (max-width: 480px) {{
                    .button {{
                        display: block;
                        margin: 10px 0;
                    }}
                }}
                .confirm-button {{
                    background-color: #28a745;
                }}
                .reject-button {{
                    background-color: #dc3545;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h2>Вітаємо, {userName}!</h2>
                <p>Адміністратор хоче змінити вашу роль у системі на: <strong>{newRole}</strong></p>
                <p>Будь ласка, підтвердіть чи відхиліть цю зміну:</p>
                
                <div style='margin: 20px 0;'>
                    <!-- Видалено target='_blank' з обох посилань -->
                    <a href='{confirmationLink}' class='button confirm-button'>
                        Підтвердити зміну ролі
                    </a>
                    <a href='{rejectLink}' class='button reject-button'>
                        Відхилити зміну ролі
                    </a>
                </div>
                
                <p style='margin-top: 20px; color: #666;'>
                    З повагою,<br>
                    Команда підтримки ветеранів
                </p>
            </div>
        </body>
        </html>";

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailSettings.FromEmail, _emailSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }

}
