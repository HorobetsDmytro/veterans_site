using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Models;

namespace veterans_site.Services
{
    public class ConsultationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ConsultationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public ConsultationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ConsultationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessConsultations();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in ConsultationBackgroundService: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task ProcessConsultations()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<VeteranSupportDBContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var currentTime = DateTime.Now;

            try
            {
                // Оновлення статусів консультацій
                var consultationsToUpdate = await context.Consultations
                    .Where(c => c.Status != ConsultationStatus.Cancelled &&
                               c.Status != ConsultationStatus.Completed)
                    .ToListAsync();

                foreach (var consultation in consultationsToUpdate)
                {
                    var consultationEndTime = consultation.Format == ConsultationFormat.Individual
                        ? consultation.EndDateTime
                        : consultation.DateTime.AddMinutes(consultation.Duration);

                    // Якщо консультація вже завершилась
                    if (consultationEndTime <= currentTime)
                    {
                        consultation.Status = ConsultationStatus.Completed;
                        _logger.LogInformation($"Consultation {consultation.Id} marked as Completed");
                    }
                    // Якщо консультація зараз проходить
                    else if (consultation.DateTime <= currentTime && currentTime < consultationEndTime)
                    {
                        if (consultation.Status != ConsultationStatus.InProgress)
                        {
                            consultation.Status = ConsultationStatus.InProgress;
                            _logger.LogInformation($"Consultation {consultation.Id} marked as InProgress");
                        }
                    }
                }

                // Відправка нагадувань про консультації
                var notificationTime = currentTime.AddMinutes(5);
                var upcomingConsultations = await context.Consultations
                    .Include(c => c.Slots)
                        .ThenInclude(s => s.User)
                    .Include(c => c.Bookings)
                        .ThenInclude(b => b.User)
                    .Where(c => c.Status == ConsultationStatus.Planned &&
                               c.DateTime > currentTime &&
                               c.DateTime <= notificationTime &&
                               !c.NotificationSent)
                    .ToListAsync();

                foreach (var consultation in upcomingConsultations)
                {
                    var usersToNotify = new List<(ApplicationUser User, DateTime Time)>();

                    if (consultation.Format == ConsultationFormat.Individual)
                    {
                        foreach (var slot in consultation.Slots.Where(s => s.IsBooked && s.User != null))
                        {
                            if (slot.DateTime <= notificationTime && slot.DateTime > currentTime)
                            {
                                usersToNotify.Add((slot.User, slot.DateTime));
                            }
                        }
                    }
                    else
                    {
                        foreach (var booking in consultation.Bookings.Where(b => b.User != null))
                        {
                            usersToNotify.Add((booking.User, consultation.DateTime));
                        }
                    }

                    var zoomId = consultation.Id.ToString().PadLeft(10, '0');
                    var zoomUrl = consultation.Mode == ConsultationMode.Online
                        ? $"https://zoom.us/j/{zoomId}"
                        : null;

                    foreach (var (user, time) in usersToNotify)
                    {
                        try
                        {
                            await emailService.SendConsultationStartNotificationAsync(
                                user.Email,
                                $"{user.FirstName} {user.LastName}",
                                consultation.Title,
                                time,
                                consultation.Mode == ConsultationMode.Online,
                                consultation.Location,
                                zoomUrl
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error sending notification to {user.Email}: {ex.Message}");
                        }
                    }

                    consultation.NotificationSent = true;
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing consultations: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}
