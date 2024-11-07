using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Models;

namespace veterans_site.Services
{
    public class EventBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EventBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

        public EventBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<EventBackgroundService> logger)
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
                    var now = DateTime.Now;
                    // Чекаємо до наступної перевірки о 00:00
                    var tomorrow = now.Date.AddDays(1);
                    var delay = tomorrow - now;
                    await Task.Delay(delay, stoppingToken);

                    await ProcessEvents();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in EventBackgroundService: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task ProcessEvents()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<VeteranSupportDBContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var currentDate = DateTime.Now.Date;

            // Отримуємо всі події, які відбудуться сьогодні
            var todayEvents = await context.Events
                .Include(e => e.EventParticipants)
                    .ThenInclude(ep => ep.User)
                .Where(e => e.Date.Date == currentDate
                        && e.Status == EventStatus.Planned)
                .ToListAsync();

            foreach (var evt in todayEvents)
            {
                foreach (var participant in evt.EventParticipants)
                {
                    try
                    {
                        await emailService.SendEventReminderAsync(
                            participant.User.Email,
                            $"{participant.User.FirstName} {participant.User.LastName}",
                            evt
                        );
                        _logger.LogInformation($"Sent reminder for event {evt.Id} to {participant.User.Email}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to send reminder to {participant.User.Email}: {ex.Message}");
                    }
                }
            }

            // Оновлюємо статуси подій
            await UpdateEventStatuses(context);
        }

        private async Task UpdateEventStatuses(VeteranSupportDBContext context)
        {
            var currentTime = DateTime.Now;
            var eventsToUpdate = await context.Events
                .Where(e => e.Status == EventStatus.Planned || e.Status == EventStatus.InProgress)
                .ToListAsync();

            foreach (var evt in eventsToUpdate)
            {
                if (evt.Status == EventStatus.Planned && evt.Date <= currentTime)
                {
                    evt.Status = EventStatus.InProgress;
                    _logger.LogInformation($"Event {evt.Id} status changed to InProgress");
                }
                else if (evt.Status == EventStatus.InProgress &&
                         evt.Date.AddMinutes(evt.Duration) <= currentTime)
                {
                    evt.Status = EventStatus.Completed;
                    _logger.LogInformation($"Event {evt.Id} status changed to Completed");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
