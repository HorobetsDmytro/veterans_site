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
        private readonly string _zoomMeetingUrl = "https://zoom.us/j/";

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
            var notificationTime = currentTime.AddMinutes(5);

            // Знаходимо консультації, які починаються через 5 хвилин і для яких ще не відправлено повідомлення
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
                    ? $"{_zoomMeetingUrl}{zoomId}"
                    : null;

                var allNotificationsSent = true;

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
                        allNotificationsSent = false;
                    }
                }

                if (allNotificationsSent)
                {
                    consultation.NotificationSent = true;
                    await context.SaveChangesAsync();
                }
            }

            var consultationsToStart = await context.Consultations
                .Where(c => c.Status == ConsultationStatus.Planned &&
                           c.DateTime <= currentTime)
                .ToListAsync();

            foreach (var consultation in consultationsToStart)
            {
                consultation.Status = ConsultationStatus.InProgress;
            }

            var consultationsToComplete = await context.Consultations
        .Where(c => c.Status == ConsultationStatus.InProgress)
        .ToListAsync();

            foreach (var consultation in consultationsToComplete)
            {
                DateTime endTime;

                if (consultation.Format == ConsultationFormat.Individual)
                {
                    if (consultation.EndDateTime.HasValue)
                    {
                        endTime = consultation.EndDateTime.Value;
                    }
                    else continue;
                }
                else
                {
                    endTime = consultation.DateTime.AddMinutes(consultation.Duration);
                }

                if (currentTime >= endTime)
                {
                    consultation.Status = ConsultationStatus.Completed;
                }
            }

            // Зберігаємо зміни
            if (consultationsToComplete.Any(c => c.Status == ConsultationStatus.Completed))
            {
                try
                {
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating consultation statuses: {ex.Message}");
                }
            }
        }

        private DateTime? GetConsultationEndTime(Consultation consultation)
        {
            if (consultation.Format == ConsultationFormat.Individual)
            {
                return consultation.EndDateTime;
            }

            if (consultation.Duration > 0)
            {
                return consultation.DateTime.AddMinutes(consultation.Duration);
            }

            return null;
        }
    }
}
