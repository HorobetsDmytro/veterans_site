using Microsoft.AspNetCore.SignalR;
using veterans_site.Interfaces;

namespace veterans_site.Hubs
{
    public class TaxiHub : Hub
    {
        private readonly ILogger<TaxiHub> _logger;
        private readonly ISocialTaxiRepository _taxiRepository;

        public TaxiHub(ILogger<TaxiHub> logger, ISocialTaxiRepository taxiRepository)
        {
            _logger = logger;
            _taxiRepository = taxiRepository;
        }

        public async Task JoinRide(string rideId)
        {
            _logger.LogInformation($"Клієнт {Context.ConnectionId} приєднався до групи поїздки {rideId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, rideId);
            await Clients.Caller.SendAsync("JoinedRide", new { rideId = rideId });
        }
        
        public async Task LeaveRide(string rideId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, rideId);
            Console.WriteLine($"Клієнт {Context.ConnectionId} вийшов з групи поїздки {rideId}");
        }
        
        public async Task JoinDriversGroup()
        {
            _logger.LogInformation($"Клієнт {Context.ConnectionId} приєднався до групи водіїв");
            await Groups.AddToGroupAsync(Context.ConnectionId, "drivers");
            await Clients.Caller.SendAsync("JoinedDriversGroup");
        }

        public async Task GoOnline()
        {
            _logger.LogInformation($"Водій {Context.ConnectionId} перейшов у статус онлайн");
            await Groups.AddToGroupAsync(Context.ConnectionId, "drivers");
        }

        public async Task GoOffline()
        {
            _logger.LogInformation($"Водій {Context.ConnectionId} перейшов у статус офлайн");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "drivers");
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Клієнт {Context.ConnectionId} підключився до TaxiHub");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"Клієнт {Context.ConnectionId} відключився від TaxiHub");
            if (exception != null)
            {
                _logger.LogError($"Причина відключення: {exception.Message}");
            }
            await base.OnDisconnectedAsync(exception);
        }
        
        public async Task NotifyNewScheduledRide(int rideId, string veteranName, string startAddress, string endAddress, 
            double startLat, double startLng, double endLat, double endLng, double distanceKm, string scheduledTime, List<string> carTypes)
        {
            var eligibleDriversIds = await _taxiRepository.GetDriverIdsByCarTypesAsync(carTypes);
    
            foreach (var driverId in eligibleDriversIds)
            {
                await Clients.User(driverId).SendAsync("NewScheduledRide", new
                {
                    rideId,
                    veteranName,
                    startAddress,
                    endAddress,
                    startLat,
                    startLng,
                    endLat,
                    endLng,
                    distanceKm,
                    estimatedPrice = 0,
                    scheduledTime,
                    carTypes
                });
            }
        }

        public async Task NotifyScheduledRideActive(
            string rideId, 
            string veteranName, 
            string startAddress, 
            string endAddress,
            double startLat,
            double startLng,
            double endLat,
            double endLng,
            double distanceKm,
            string scheduledTime,
            List<string> carTypes)
        {
            var eligibleDriverIds = await _taxiRepository.GetDriverIdsByCarTypesAsync(carTypes);
    
            foreach (var driverId in eligibleDriverIds)
            {
                await Clients.User(driverId).SendAsync("ScheduledRideActive", new
                {
                    rideId,
                    veteranName,
                    startAddress,
                    endAddress,
                    startLat,
                    startLng,
                    endLat,
                    endLng,
                    distanceKm,
                    scheduledTime,
                    carTypes
                });
            }
        }
        
        public async Task NotifyRideCanceled(int rideId, string message)
        {
            Console.WriteLine($"Відправляємо повідомлення про скасування поїздки {rideId}: {message}");
    
            await Clients.Group(rideId.ToString()).SendAsync("RideCanceled", new
            {
                rideId = rideId,
                message = message ?? "Поїздку скасовано"
            });
        }
    }
}