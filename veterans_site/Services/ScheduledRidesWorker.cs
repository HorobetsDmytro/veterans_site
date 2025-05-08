using Microsoft.AspNetCore.SignalR;
using veterans_site.Hubs;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Services;

public class ScheduledRidesWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ScheduledRidesWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public ScheduledRidesWorker(
        IServiceProvider services,
        ILogger<ScheduledRidesWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Фоновий сервіс запланованих поїздок запущено");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ActivateScheduledRides();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при активації запланованих поїздок");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ActivateScheduledRides()
    {
        using var scope = _services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISocialTaxiRepository>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TaxiHub>>();

        var activationThreshold = DateTime.Now.AddMinutes(-1);
        var rides = await repository.GetRidesForActivationAsync();

        foreach (var ride in rides)
        {
            _logger.LogInformation($"Активація запланованої поїздки {ride.Id}, запланований час: {ride.ScheduledTime}");

            bool hasAssignedDriver = !string.IsNullOrEmpty(ride.DriverId);

            if (hasAssignedDriver)
            {
                await repository.UpdateRideStatusAsync(ride.Id, TaxiRideStatus.Accepted);

                _logger.LogInformation($"Поїздка {ride.Id} автоматично активована для водія {ride.DriverId}");

                await hubContext.Clients.User(ride.DriverId).SendAsync("ScheduledRideActivated", new
                {
                    rideId = ride.Id,
                    veteranName = $"{ride.Veteran.FirstName} {ride.Veteran.LastName}",
                    startAddress = ride.StartAddress,
                    endAddress = ride.EndAddress,
                    startLat = ride.StartLatitude,
                    startLng = ride.StartLongitude,
                    endLat = ride.EndLatitude,
                    endLng = ride.EndLongitude,
                    distanceKm = ride.EstimatedDistance,
                    estimatedPrice = 0,
                    scheduledTime = ride.ScheduledTime?.ToString("dd.MM.yyyy HH:mm"),
                    driverAssigned = true,
                    isScheduled = true,
                    message = "Час вашої запланованої поїздки настав! Ви можете почати виконання!"
                });

                await hubContext.Clients.User(ride.VeteranId).SendAsync("ScheduledRideActivated", new
                {
                    rideId = ride.Id,
                    scheduledTime = ride.ScheduledTime?.ToString("dd.MM.yyyy HH:mm"),
                    driverAssigned = true,
                    message = "Вашу заплановану поїздку активовано! Водій готовий почати поїздку."
                });
            }
            else
            {
                await repository.UpdateRideStatusAsync(ride.Id, TaxiRideStatus.Requested);

                await hubContext.Clients.Group("drivers").SendAsync("ScheduledRideActive", new
                {
                    rideId = ride.Id,
                    veteranName = $"{ride.Veteran.FirstName} {ride.Veteran.LastName}",
                    startAddress = ride.StartAddress,
                    endAddress = ride.EndAddress,
                    startLat = ride.StartLatitude,
                    startLng = ride.StartLongitude,
                    endLat = ride.EndLatitude,
                    endLng = ride.EndLongitude,
                    distanceKm = ride.EstimatedDistance,
                    estimatedPrice = 0,
                    scheduledTime = ride.ScheduledTime?.ToString("dd.MM.yyyy HH:mm"),
                    isScheduled = true
                });

                await hubContext.Clients.User(ride.VeteranId).SendAsync("ScheduledRideActivated", new
                {
                    rideId = ride.Id,
                    scheduledTime = ride.ScheduledTime?.ToString("dd.MM.yyyy HH:mm"),
                    driverAssigned = false,
                    message = "Вашу заплановану поїздку активовано! Шукаємо водія."
                });
            }
        }
    }
}