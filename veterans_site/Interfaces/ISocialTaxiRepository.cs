using veterans_site.Models;

namespace veterans_site.Interfaces;

public interface ISocialTaxiRepository
{
    Task<IEnumerable<ApplicationUser>> GetAvailableDriversAsync();
    Task<IEnumerable<ApplicationUser>> GetAllDriversAsync();
    Task<TaxiRide> CreateRideAsync(TaxiRide ride);
    Task<TaxiRide> GetRideByIdAsync(int id);
    Task<IEnumerable<TaxiRide>> GetRidesForVeteranAsync(string veteranId);
    Task<TaxiRide> UpdateRideAsync(TaxiRide ride);
    Task<bool> AssignDriverToRideAsync(int rideId, string driverId);
    Task<bool> UpdateRideStatusAsync(int rideId, TaxiRideStatus status);
    Task<ApplicationUser> GetDriverByIdAsync(string id);
    Task<ApplicationUser> GetVeteranByIdAsync(string id);
    Task<bool> UpdateDriverLocationAsync(string driverId, double latitude, double longitude);
    Task UpdateRidePickupTimeAsync(int rideId, DateTime pickupTime);
    Task UpdateRideCompleteTimeAsync(int rideId, DateTime completeTime);
    Task UpdateRideAcceptTimeAsync(int rideId, DateTime acceptTime);
    Task<IEnumerable<TaxiRide>> GetAvailableRidesAsync();
    Task<IEnumerable<TaxiRide>> GetRidesForDriverAsync(string driverId);
    Task<TaxiRide> GetActiveRideForDriverAsync(string driverId);
    Task<List<TaxiRide>> GetActiveRideRequestsAsync();
    Task<List<TaxiRide>> GetScheduledRidesAsync();
    Task<List<TaxiRide>> GetActiveScheduledRidesAsync();
    Task<bool> AssignDriverToScheduledRideAsync(int rideId, string driverId);
    Task<List<TaxiRide>> GetDriverAcceptedScheduledRidesAsync(string driverId);
    Task<List<TaxiRide>> GetRidesForActivationAsync();
}