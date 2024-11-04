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
                    await ProcessEvents();
                    await Task.Delay(_checkInterval, stoppingToken);
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
            var currentTime = DateTime.Now;

            var eventsToUpdate = await context.Events
                .Where(e => e.Status == EventStatus.Planned || e.Status == EventStatus.InProgress)
                .Include(e => e.EventParticipants)
                    .ThenInclude(ep => ep.User)
                .ToListAsync();

            foreach (var evt in eventsToUpdate)
            {
                try
                {
                    // Подія починається
                    if (evt.Status == EventStatus.Planned && evt.Date <= currentTime)
                    {
                        evt.Status = EventStatus.InProgress;
                        _logger.LogInformation($"Event {evt.Id} status changed to InProgress");
                    }
                    // Подія закінчується (використовуємо тривалість)
                    else if (evt.Status == EventStatus.InProgress &&
                             evt.Date.AddMinutes(evt.Duration) <= currentTime)
                    {
                        evt.Status = EventStatus.Completed;
                        _logger.LogInformation($"Event {evt.Id} status changed to Completed");
                    }

                    // Відправка нагадувань учасникам за день до події
                    if (evt.Status == EventStatus.Planned &&
                        evt.Date.Date == currentTime.Date.AddDays(1))
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
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Failed to send reminder to {participant.User.Email}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing event {evt.Id}: {ex.Message}");
                }
            }

            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving event updates: {ex.Message}");
            }
        }
    }
}
