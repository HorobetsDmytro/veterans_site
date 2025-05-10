using Microsoft.AspNetCore.SignalR;
using veterans_site.Hubs;
using veterans_site.Interfaces;

namespace veterans_site.Services
{
    public class ScheduledRidesService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduledRidesService> _logger;

        public ScheduledRidesService(
            IServiceProvider serviceProvider,
            ILogger<ScheduledRidesService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled Rides Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckScheduledRides();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking scheduled rides.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Scheduled Rides Service is stopping.");
        }

        private async Task CheckScheduledRides()
{
    _logger.LogInformation("Checking for scheduled rides that need to be activated...");
    
    using (var scope = _serviceProvider.CreateScope())
    {
        var taxiRepository = scope.ServiceProvider.GetRequiredService<ISocialTaxiRepository>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TaxiHub>>();
        var hub = scope.ServiceProvider.GetRequiredService<TaxiHub>();
        
        var activeScheduledRides = await taxiRepository.GetActiveScheduledRidesAsync();
        
        foreach (var ride in activeScheduledRides)
        {
            try
            {
                _logger.LogInformation($"Found scheduled ride {ride.Id} that needs to be activated");
                
                var carTypes = ride.CarTypes ?? new List<string>();
                
                var eligibleDriverIds = await taxiRepository.GetDriverIdsByCarTypesAsync(carTypes);
                
                foreach (var driverId in eligibleDriverIds)
                {
                    await hubContext.Clients.User(driverId).SendAsync("NotifyScheduledRideActive", new
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
                        scheduledTime = ride.ScheduledTime?.ToString("dd.MM.yyyy HH:mm"),
                        carTypes = ride.CarTypes
                    });
                }
                
                if (!string.IsNullOrEmpty(ride.DriverId))
                {
                    await hubContext.Clients.User(ride.DriverId).SendAsync("RideRequestForDriver", new
                    {
                        rideId = ride.Id,
                        veteranName = $"{ride.Veteran.FirstName} {ride.Veteran.LastName}",
                        startAddress = ride.StartAddress,
                        endAddress = ride.EndAddress,
                        distanceKm = ride.EstimatedDistance,
                        estimatedPrice = 0
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing scheduled ride {ride.Id}");
            }
        }
    }
}
    }
}