using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Repositories;

public class SocialTaxiRepository : ISocialTaxiRepository
{
    private readonly VeteranSupportDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
        
    public SocialTaxiRepository(VeteranSupportDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    public async Task<IEnumerable<ApplicationUser>> GetAvailableDriversAsync()
    {
        var drivers = await _userManager.GetUsersInRoleAsync("Driver");
        var availableDrivers = drivers.Where(d => d.IsAvailable == true).ToList();
    
        Console.WriteLine($"Found {availableDrivers.Count} available drivers");
    
        return availableDrivers;
    }
    
    public async Task<IEnumerable<ApplicationUser>> GetAllDriversAsync()
    {
        return await _userManager.GetUsersInRoleAsync("Driver");
    }
    
    public async Task<TaxiRide> CreateRideAsync(TaxiRide ride)
    {
        _context.TaxiRides.Add(ride);
        await _context.SaveChangesAsync();
        return ride;
    }
    
    public async Task<TaxiRide> GetRideByIdAsync(int id)
    {
        return await _context.TaxiRides
            .Include(r => r.Driver)
            .Include(r => r.Veteran)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
    
    public async Task<IEnumerable<TaxiRide>> GetRidesForVeteranAsync(string veteranId)
    {
        return await _context.TaxiRides
            .Include(r => r.Driver)
            .Where(r => r.VeteranId == veteranId)
            .OrderByDescending(r => r.RequestTime)
            .ToListAsync();
    }
    
    public async Task<bool> AssignDriverToRideAsync(int rideId, string driverId)
    {
        var ride = await _context.TaxiRides.FindAsync(rideId);
        var driver = await _context.Users.FindAsync(driverId);
    
        if (ride == null || driver == null)
            return false;
        
        ride.DriverId = driverId;
        ride.Status = TaxiRideStatus.Accepted;
        ride.AcceptTime = DateTime.Now;
    
        driver.IsAvailable = false;
    
        await _context.SaveChangesAsync();
    
        _context.Entry(ride).State = EntityState.Detached;
    
        return true;
    }
    
    public async Task<bool> UpdateRideStatusAsync(int rideId, TaxiRideStatus status)
    {
        var ride = await _context.TaxiRides.FindAsync(rideId);
        
        if (ride == null)
            return false;
            
        ride.Status = status;
        
        switch (status)
        {
            case TaxiRideStatus.DriverArriving:
                break;
            case TaxiRideStatus.InProgress:
                ride.PickupTime = DateTime.Now;
                break;
            case TaxiRideStatus.Completed:
                ride.CompleteTime = DateTime.Now;
                
                var driver = await _context.Users.FindAsync(ride.DriverId);
                if (driver != null)
                {
                    driver.IsAvailable = true;
                }
                break;
            case TaxiRideStatus.Canceled:
                var canceledDriver = await _context.Users.FindAsync(ride.DriverId);
                if (canceledDriver != null)
                {
                    canceledDriver.IsAvailable = true;
                }
                break;
        }
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<ApplicationUser> GetDriverByIdAsync(string id)
    {
        return await _context.Users.FindAsync(id);
    }
    
    public async Task<ApplicationUser> GetVeteranByIdAsync(string id)
    {
        return await _context.Users.FindAsync(id);
    }
    
    public async Task<bool> UpdateDriverLocationAsync(string driverId, double latitude, double longitude)
    {
        var driver = await _context.Users.FindAsync(driverId);
        
        if (driver == null)
            return false;
            
        driver.CurrentLatitude = latitude;
        driver.CurrentLongitude = longitude;
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task UpdateRidePickupTimeAsync(int rideId, DateTime pickupTime)
    {
        var ride = await _context.TaxiRides.FindAsync(rideId);
        if (ride != null)
        {
            ride.PickupTime = pickupTime;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateRideCompleteTimeAsync(int rideId, DateTime completeTime)
    {
        var ride = await _context.TaxiRides.FindAsync(rideId);
        if (ride != null)
        {
            ride.CompleteTime = completeTime;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<IEnumerable<TaxiRide>> GetAvailableRidesAsync()
    {
        return await _context.TaxiRides
            .Include(r => r.Veteran)
            .Where(r => r.Status == TaxiRideStatus.Requested)
            .OrderByDescending(r => r.RequestTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaxiRide>> GetRidesForDriverAsync(string driverId)
    {
        return await _context.TaxiRides
            .Include(r => r.Veteran)
            .Where(r => r.DriverId == driverId)
            .OrderByDescending(r => r.RequestTime)
            .ToListAsync();
    }

    public async Task<TaxiRide> GetActiveRideForDriverAsync(string driverId)
    {
        return await _context.TaxiRides
            .Include(r => r.Veteran)
            .FirstOrDefaultAsync(r => r.DriverId == driverId && 
              (r.Status == TaxiRideStatus.Accepted || 
               r.Status == TaxiRideStatus.DriverArriving || 
               r.Status == TaxiRideStatus.InProgress));
    }

    public async Task<bool> AssignDriverToScheduledRideAsync(int rideId, string driverId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var ride = await _context.TaxiRides
                .FirstOrDefaultAsync(r => r.Id == rideId);

            if (ride == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(ride.DriverId))
            {
                if (ride.DriverId != driverId)
                {
                    return false;
                }
            
                await transaction.CommitAsync();
                return true;
            }

            ride.DriverId = driverId;
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<List<TaxiRide>> GetActiveScheduledRidesAsync()
    {
        return await _context.TaxiRides
            .Include(r => r.Veteran)
            .Where(r => r.ScheduledTime.HasValue && 
                        r.ScheduledTime.Value == DateTime.Now && 
                        r.Status == TaxiRideStatus.Requested)
            .ToListAsync();
    }
    
    public async Task<List<TaxiRide>> GetDriverAcceptedScheduledRidesAsync(string driverId)
    {
        return await _context.TaxiRides
            .Include(r => r.Veteran)
            .Where(r => r.ScheduledTime.HasValue && 
                        r.Status == TaxiRideStatus.Requested &&
                        r.DriverId == driverId &&
                        r.ScheduledTime > DateTime.Now)
            .OrderBy(r => r.ScheduledTime)
            .ToListAsync();
    }
    
    public async Task<List<TaxiRide>> GetRidesForActivationAsync()
    {
        return await _context.TaxiRides
            .Include(r => r.Veteran)
            .Where(r => r.ScheduledTime.HasValue && 
                        r.ScheduledTime.Value == DateTime.Now && 
                        r.Status == TaxiRideStatus.Requested &&
                        (r.DriverId != null || r.DriverId == null))
            .ToListAsync();
    }
    
    public async Task<List<TaxiRide>> GetActiveScheduledRidesForDriverTypeAsync(string carType)
    {
        var driverCarTypes = carType.Split(',').Select(t => t.Trim()).ToList();

        var rides = await _context.TaxiRides
            .Include(r => r.Veteran)
            .Where(r => r.Status == TaxiRideStatus.Requested && 
                        r.ScheduledTime.HasValue)
            .ToListAsync();

        var filteredRides = rides.Where(r => 
            r.CarTypes != null && 
            r.CarTypes.All(requiredType => driverCarTypes.Contains(requiredType))
        ).ToList();

        return filteredRides;
    }

    public async Task<List<TaxiRide>> GetScheduledRidesForDriverTypeAsync(string carType)
    {
        var driverCarTypes = carType.Split(',').Select(t => t.Trim()).ToList();

        var rides = await _context.TaxiRides
            .Include(r => r.Veteran)
            .Where(r => r.Status == TaxiRideStatus.Requested && 
                        r.ScheduledTime.HasValue)
            .ToListAsync();

        var filteredRides = rides.Where(r => 
            r.CarTypes != null && 
            r.CarTypes.All(requiredType => driverCarTypes.Contains(requiredType))
        ).ToList();

        return filteredRides;
    }

    public async Task<List<string>> GetDriverIdsByCarTypesAsync(List<string> carTypes)
    {
        if (carTypes == null || !carTypes.Any())
        {
            return new List<string>();
        }

        var activeDrivers = await _context.Users
            .Where(u => u.IsActive && u.CarType != null)
            .ToListAsync();

        var eligibleDrivers = activeDrivers.Where(driver =>
        {
            if (string.IsNullOrEmpty(driver.CarType))
                return false;
            
            var driverCarTypes = driver.CarType.Split(',').Select(t => t.Trim()).ToList();
            
            return carTypes.All(reqType => driverCarTypes.Contains(reqType));
        }).ToList();

        return eligibleDrivers.Select(d => d.Id).ToList();
    }

    public async Task<List<TaxiRide>> GetActiveRideRequestsForDriverTypeAsync(string driverCarType)
    {
        if (string.IsNullOrEmpty(driverCarType))
        {
            return new List<TaxiRide>();
        }

        var driverCarTypes = driverCarType.Split(',').Select(t => t.Trim()).ToList();

        var rides = await _context.TaxiRides
            .Include(r => r.Veteran)
            .Where(r => r.Status == TaxiRideStatus.Requested)
            .ToListAsync();

        var filteredRides = rides.Where(r => 
            r.CarTypes != null && r.CarTypes.Any() && 
            r.CarTypes.All(requiredType => driverCarTypes.Contains(requiredType))
        ).ToList();

        return filteredRides;
    }
    
    public async Task<List<TaxiRide>> GetActiveScheduledRidesForDriverAsync(string driverId)
    {
        var now = DateTime.Now;
    
        return await _context.TaxiRides
            .Include(r => r.Veteran)
            .Where(r => r.DriverId == driverId &&
                        r.ScheduledTime.HasValue &&
                        r.ScheduledTime.Value > now &&
                        r.Status == TaxiRideStatus.Requested)
            .ToListAsync();
    }
}