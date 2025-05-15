using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using veterans_site.Models;
using veterans_site.Extensions;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace veterans_site.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IConfiguration _configuration;

        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, IConfiguration configuration)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _configuration = configuration;
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

        public async Task SendNewConsultationNotificationAsync(string toEmail, string veteranName, Consultation consultation)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = "Нова консультація доступна";

                var locationHtml = consultation.Mode == ConsultationMode.Offline
                    ? $"<li style='margin-bottom: 8px;'><strong>Місце проведення:</strong> {consultation.Location}</li>"
                    : "";

                var slotsHtml = "";
                if (consultation.Format == ConsultationFormat.Individual && consultation.Slots != null && consultation.Slots.Any())
                {
                    var slotsTable = @"
                <div style='margin-top: 15px;'>
                    <h4 style='color: #2c3e50; margin-bottom: 10px;'>Доступні слоти:</h4>
                    <table style='width: 100%; border-collapse: collapse; background-color: white;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 10px; border: 1px solid #dee2e6; text-align: left;'>Дата та час</th>
                            </tr>
                        </thead>
                        <tbody>";

                    foreach (var slot in consultation.Slots.OrderBy(s => s.DateTime))
                    {
                        if (!slot.IsBooked)
                        {
                            slotsTable += $@"
                        <tr>
                            <td style='padding: 10px; border: 1px solid #dee2e6;'>{slot.DateTime:dd.MM.yyyy HH:mm}</td>
                        </tr>";
                        }
                    }

                    slotsTable += @"
                        </tbody>
                    </table>
                </div>";

                    slotsHtml = slotsTable;
                }

                var builder = new BodyBuilder();
                builder.HtmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif; margin: 0; padding: 20px;'>
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                    <h2 style='color: #2c3e50;'>Вітаємо, {veteranName}!</h2>
                    <p>Доступна нова консультація:</p>
                    
                    <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                        <h3 style='color: #2c3e50; margin-top: 0;'>{consultation.Title}</h3>
                        <p style='color: #34495e;'>{consultation.Description}</p>
                        
                        <ul style='list-style: none; padding: 0;'>
                            <li style='margin-bottom: 8px;'>
                                <strong>Спеціаліст:</strong> {consultation.SpecialistName}
                            </li>
                            <li style='margin-bottom: 8px;'>
                                <strong>Тип консультації:</strong> {consultation.Type.GetDisplayName()}
                            </li>
                            <li style='margin-bottom: 8px;'>
                                <strong>Формат проведення:</strong> 
                                <span style='display: inline-block; padding: 3px 8px; border-radius: 3px; {(consultation.Mode == ConsultationMode.Online ? "background-color: #e8f5e9; color: #2e7d32;" : "background-color: #fff3e0; color: #f57c00;")}'>{consultation.Mode.GetDisplayName()}</span>
                            </li>
                            <li style='margin-bottom: 8px;'>
                                <strong>Формат консультації:</strong> {consultation.Format.GetDisplayName()}
                            </li>
                            <li style='margin-bottom: 8px;'>
                                <strong>Тривалість:</strong> {consultation.Duration} хвилин
                            </li>
                            <li style='margin-bottom: 8px;'>
                                <strong>Ціна:</strong> {consultation.Price:C}
                            </li>
                            {locationHtml}
                        </ul>

                        {slotsHtml}
                    </div>

                    <div style='background-color: #e3f2fd; padding: 15px; border-radius: 5px; margin-top: 20px;'>
                        <p style='margin: 0; color: #1976d2;'>
                            <strong>Примітка:</strong> Для запису на консультацію, будь ласка, відвідайте наш веб-сайт.
                        </p>
                    </div>

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

                _logger.LogInformation($"Notification about new consultation sent to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending new consultation notification: {ex.Message}");
                throw;
            }
        }

        public async Task SendBookingRequestToSpecialistAsync(string specialistEmail, string specialistName,
        string userFullName, string consultationTitle, DateTime consultationTime,
        string confirmLink, string rejectLink)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
                email.To.Add(MailboxAddress.Parse(specialistEmail));
                email.Subject = "Новий запит на консультацію";

                var builder = new BodyBuilder();
                builder.HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; margin: 0; padding: 20px;'>
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                        <h2 style='color: #2c3e50;'>Вітаємо, {specialistName}!</h2>
                        <p>Користувач <strong>{userFullName}</strong> бажає записатись на вашу консультацію:</p>
                        
                        <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                            <h3 style='color: #2c3e50; margin-top: 0;'>{consultationTitle}</h3>
                            <p><strong>Дата та час:</strong> {consultationTime:dd.MM.yyyy HH:mm}</p>
                        </div>

                        <div style='margin: 20px 0; text-align: center;'>
                            <a href='{confirmLink}' style='display: inline-block; margin: 10px; padding: 10px 20px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px;'>Підтвердити запис</a>
                            <a href='{rejectLink}' style='display: inline-block; margin: 10px; padding: 10px 20px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px;'>Відхилити запит</a>
                        </div>

                        <p style='color: #7f8c8d;'>З повагою,<br>Система підтримки ветеранів</p>
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
            catch (Exception ex)
            {
                _logger.LogError($"Error sending booking request email: {ex.Message}");
                throw;
            }
        }

        public async Task SendBookingRejectedEmailAsync(string userEmail, string userName, string consultationTitle)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(userEmail));
            email.Subject = "Запит на консультацію відхилено";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif; margin: 0; padding: 20px;'>
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                    <h2 style='color: #2c3e50;'>Вітаємо, {userName}!</h2>
                    <p>На жаль, ваш запит на консультацію ""{consultationTitle}"" було відхилено.</p>
                    <p>Будь ласка, спробуйте записатись на інший час або зв'яжіться зі спеціалістом для отримання додаткової інформації.</p>
                    <br>
                    <p style='color: #7f8c8d;'>З повагою,<br>Система підтримки ветеранів</p>
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

        public async Task SendConsultationStartNotificationAsync(
            string toEmail,
            string userName,
            string consultationTitle,
            DateTime startTime,
            bool isOnline,
            string location = null,
            string zoomUrl = null)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"Нагадування про консультацію \"{consultationTitle}\"";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
        <html>
        <body style='font-family: Arial, sans-serif; margin: 0; padding: 20px;'>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                <h2 style='color: #2c3e50;'>Вітаємо, {userName}!</h2>
                <p>Нагадуємо, що через 5 хвилин розпочнеться ваша консультація ""{consultationTitle}"".</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                    <p><strong>Час початку:</strong> {startTime:dd.MM.yyyy HH:mm}</p>
                    {(isOnline ?
                                $@"<p>Консультація проходитиме онлайн.
                           <br><strong>Посилання на Zoom:</strong> 
                           <a href='{zoomUrl}' style='color: #007bff;'>{zoomUrl}</a>
                           <br>Будь ласка, приєднайтесь за 2-3 хвилини до початку.</p>" :
                                $"<p><strong>Місце проведення:</strong> {location}</p>")}
                </div>

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
        }

        public async Task SendEventRegistrationConfirmationAsync(string toEmail, string userName, Event evt)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"Підтвердження реєстрації на подію \"{evt.Title}\"";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
        <html>
        <body style='font-family: Arial, sans-serif; margin: 0; padding: 20px;'>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                <h2 style='color: #2c3e50;'>Вітаємо, {userName}!</h2>
                <p>Ви успішно зареєструвалися на подію ""{evt.Title}"".</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                    <p><strong>Дата та час:</strong> {evt.Date:dd.MM.yyyy HH:mm}</p>
                    <p><strong>Місце проведення:</strong> {evt.Location}</p>
                    <p><strong>Категорія:</strong> {evt.Category.GetDisplayName()}</p>
                </div>

                <p>Не забудьте прийти вчасно!</p>
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
        }

        public async Task SendEventCancellationNotificationAsync(string toEmail, string userName, Event evt)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"Скасування реєстрації на подію \"{evt.Title}\"";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
        <html>
        <body style='font-family: Arial, sans-serif; margin: 0; padding: 20px;'>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                <h2 style='color: #2c3e50;'>Вітаємо, {userName}!</h2>
                <p>Вашу реєстрацію на подію ""{evt.Title}"" було скасовано.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                    <p><strong>Дата та час:</strong> {evt.Date:dd.MM.yyyy HH:mm}</p>
                    <p><strong>Місце проведення:</strong> {evt.Location}</p>
                </div>

                <p>Ви можете зареєструватися на інші доступні події в нашій системі.</p>
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
        }

        public async Task SendEventReminderAsync(string toEmail, string userName, Event evt)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"Нагадування про подію \"{evt.Title}\"";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
        <html>
        <body style='font-family: Arial, sans-serif; margin: 0; padding: 20px;'>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                <h2 style='color: #2c3e50;'>Вітаємо, {{userName}}!</h2>
                <p>Нагадуємо, що завтра відбудеться подія {evt.Title}.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                    <p><strong>Дата та час:</strong> {evt.Date:dd.MM.yyyy HH:mm}</p>
                    <p><strong>Місце проведення:</strong> {evt.Location}</p>
                    <p><strong>Категорія:</strong> {evt.Category.GetDisplayName()}</p>
                </div>

                <p>Чекаємо на вас!</p>
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
        }

        public async Task SendRegistrationConfirmationAsync(string toEmail, string userName)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = "Успішна реєстрація на платформі підтримки ветеранів";

                var builder = new BodyBuilder();
                builder.HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; margin: 0; padding: 20px;'>
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                        <h2 style='color: #2c3e50;'>Вітаємо, {userName}!</h2>
                        <p>Ви успішно зареєструвалися на платформі підтримки ветеранів.</p>
                        <p>Тепер ви маєте доступ до:</p>
                        <ul>
                            <li>Запису на консультації</li>
                            <li>Реєстрації на події</li>
                            <li>Перегляду новин та оновлень</li>
                        </ul>
                        <p style='background-color: #e3f2fd; padding: 15px; border-radius: 5px; margin-top: 20px;'>
                            <strong>Примітка:</strong> Якщо ви не реєструвалися на нашій платформі, 
                            будь ласка, проігноруйте цей лист.
                        </p>
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
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending registration confirmation email: {ex.Message}");
                throw;
            }
        }
        
        public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(
                    _configuration["EmailSettings:SenderName"], 
                    _configuration["EmailSettings:FromEmail"]));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;

                var builder = new BodyBuilder();
                builder.HtmlBody = htmlBody;
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _configuration["EmailSettings:SmtpServer"], 
                    int.Parse(_configuration["EmailSettings:SmtpPort"]), 
                    SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(
                    _configuration["EmailSettings:Username"], 
                    _configuration["EmailSettings:Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation($"Email успішно надіслано на адресу {to}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка при відправці email: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> SendEmailWithFallbackAsync(
            string to, 
            string subject, 
            string htmlBody, 
            string fallbackEmail = "dmtrgorobec@gmail.com")
        {
            if (string.IsNullOrWhiteSpace(to) || !IsValidEmail(to))
            {
                _logger.LogWarning($"Недійсна адреса електронної пошти '{to}', використовуємо резервну адресу '{fallbackEmail}'");
                return await SendEmailAsync(fallbackEmail, subject, htmlBody);
            }
            
            bool result = await SendEmailAsync(to, subject, htmlBody);
            
            if (!result)
            {
                _logger.LogWarning($"Не вдалося надіслати електронний лист на '{to}', використовуємо резервну адресу '{fallbackEmail}'");
                return await SendEmailAsync(fallbackEmail, subject, htmlBody);
            }
            
            return result;
        }
        
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
                
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}