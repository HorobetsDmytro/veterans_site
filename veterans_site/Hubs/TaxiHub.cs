using Microsoft.AspNetCore.SignalR;

namespace veterans_site.Hubs
{
    public class TaxiHub : Hub
    {
        private readonly ILogger<TaxiHub> _logger;

        public TaxiHub(ILogger<TaxiHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinRide(string rideId)
        {
            _logger.LogInformation($"Клієнт {Context.ConnectionId} приєднався до групи поїздки {rideId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, rideId);
            await Clients.Caller.SendAsync("JoinedRide", new { rideId = rideId });
        }

        public async Task LeaveRide(string rideId)
        {
            _logger.LogInformation($"Клієнт {Context.ConnectionId} покинув групу поїздки {rideId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, rideId);
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
            double startLat, double startLng, double endLat, double endLng, double distanceKm, string scheduledTime)
        {
            await Clients.Group("drivers").SendAsync("NewScheduledRide", new
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
                scheduledTime
            });
        }

        public async Task NotifyScheduledRideActive(int rideId, string veteranName, string startAddress, string endAddress, 
            double startLat, double startLng, double endLat, double endLng, double distanceKm, string scheduledTime)
        {
            await Clients.Group("drivers").SendAsync("ScheduledRideActive", new
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
                scheduledTime
            });
        }
    }
}