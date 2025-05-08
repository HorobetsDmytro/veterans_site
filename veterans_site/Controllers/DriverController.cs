using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using veterans_site.Hubs;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.Services;

namespace veterans_site.Controllers;

[Authorize(Roles = "Driver")]
public class DriverController : Controller
{
    private readonly ISocialTaxiRepository _taxiRepository;
    private readonly IHubContext<TaxiHub> _hubContext;
    private readonly IEmailService _emailService;
    
    public DriverController(
        ISocialTaxiRepository taxiRepository,
        IHubContext<TaxiHub> taxiHubContext,
        IEmailService emailService)
    {
        _taxiRepository = taxiRepository;
        _hubContext = taxiHubContext;
        _emailService = emailService;
    }
    
    public IActionResult Index()
    {
        return View();
    }
    
    [HttpGet]
    public async Task<IActionResult> AvailableRides()
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var availableRides = await _taxiRepository.GetAvailableRidesAsync();
        
        return View(availableRides);
    }
    
    [HttpGet]
    public async Task<IActionResult> MyRides(int page = 1, string tab = "all")
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        const int pageSize = 10; 
        
        var allRides = await _taxiRepository.GetRidesForDriverAsync(driverId);
    
        var activeRides = allRides.Where(r => 
            r.Status == TaxiRideStatus.Accepted || 
            r.Status == TaxiRideStatus.DriverArriving || 
            r.Status == TaxiRideStatus.InProgress);
        
        var completedRides = allRides.Where(r => r.Status == TaxiRideStatus.Completed);
        var canceledRides = allRides.Where(r => r.Status == TaxiRideStatus.Canceled);
    
        var paginatedAllRides = PaginatedList<TaxiRide>.Create(
            allRides.OrderByDescending(r => r.RequestTime), 
            page, 
            pageSize);
        
        var paginatedActiveRides = PaginatedList<TaxiRide>.Create(
            activeRides.OrderByDescending(r => r.RequestTime), 
            tab == "active" ? page : 1, 
            pageSize);
        
        var paginatedCompletedRides = PaginatedList<TaxiRide>.Create(
            completedRides.OrderByDescending(r => r.RequestTime), 
            tab == "completed" ? page : 1, 
            pageSize);
        
        var paginatedCanceledRides = PaginatedList<TaxiRide>.Create(
            canceledRides.OrderByDescending(r => r.RequestTime), 
            tab == "canceled" ? page : 1, 
            pageSize);
    
        ViewBag.ActiveRides = paginatedActiveRides;
        ViewBag.CompletedRides = paginatedCompletedRides;
        ViewBag.CanceledRides = paginatedCanceledRides;
        ViewBag.ActiveTab = tab;
    
        return View(paginatedAllRides);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetActiveRide()
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    
        try
        {
            var activeRide = await _taxiRepository.GetActiveRideForDriverAsync(driverId);
        
            if (activeRide == null)
            {
                return Json(new { success = false, message = "Активна поїздка не знайдена" });
            }
        
            var rideData = new
            {
                id = activeRide.Id,
                veteranName = activeRide.Veteran.FirstName + " " + activeRide.Veteran.LastName,
                startAddress = activeRide.StartAddress,
                endAddress = activeRide.EndAddress,
                startLat = activeRide.StartLatitude,
                startLng = activeRide.StartLongitude,
                endLat = activeRide.EndLatitude,
                endLng = activeRide.EndLongitude,
                status = activeRide.Status.ToString()
            };
        
            return Json(new { success = true, ride = rideData });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> GetActiveRideRequests()
    {
        try
        {
            var rideRequests = await _taxiRepository.GetActiveRideRequestsAsync();
            var rideRequestsData = rideRequests.Select(ride => new
            {
                rideId = ride.Id,
                veteranName = ride.Veteran.FirstName + " " + ride.Veteran.LastName,
                startAddress = ride.StartAddress,
                endAddress = ride.EndAddress,
                startLat = ride.StartLatitude,
                startLng = ride.StartLongitude,
                endLat = ride.EndLatitude,
                endLng = ride.EndLongitude,
                distanceKm = ride.EstimatedDistance,
                estimatedPrice = 0,
                isScheduled = ride.ScheduledTime.HasValue,
                scheduledTime = ride.ScheduledTime?.ToString("dd.MM.yyyy HH:mm")
            }).ToList();
            
            return Json(new { success = true, rides = rideRequestsData });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> AcceptRide(int rideId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"DriverId перед призначенням: {driverId}");

        var driver = await _taxiRepository.GetDriverByIdAsync(driverId);

        if (driver == null)
        {
            Console.WriteLine("Водія не знайдено в базі даних");
            return Json(new { success = false, message = "Водія не знайдено в системі" });
        }

        var ride = await _taxiRepository.GetRideByIdAsync(rideId);

        if (ride == null || ride.Status != TaxiRideStatus.Requested)
        {
            return Json(new { success = false, message = "Поїздка недоступна для прийняття" });
        }

        try
        {
            string originalDriverId = driverId;
        
            bool assignSuccess = await _taxiRepository.AssignDriverToRideAsync(rideId, driverId);
        
            if (!assignSuccess)
            {
                return Json(new { 
                    success = false, 
                    message = "Не вдалося призначити водія до поїздки" 
                });
            }
        
            await _taxiRepository.UpdateRideStatusAsync(rideId, TaxiRideStatus.Accepted);
        
            var rideAfterAssignment = await _taxiRepository.GetRideByIdAsync(rideId);
        
            if (rideAfterAssignment.DriverId != originalDriverId)
            {
                return Json(new { 
                    success = false, 
                    message = $"Не вдалося призначити водія. Поточний DriverId: {rideAfterAssignment.DriverId}, очікувався: {originalDriverId}" 
                });
            }
        
            await _taxiRepository.UpdateRideStatusAsync(rideId, TaxiRideStatus.Accepted);
            
            await _hubContext.Clients.Group(rideId.ToString()).SendAsync("RideAccepted", new
            {
                rideId = ride.Id,
                driverName = $"{driver.FirstName} {driver.LastName}",
                driverPhone = driver.PhoneNumber,
                carModel = driver.CarModel,
                licensePlate = driver.LicensePlate,
                driverPhoto = driver.AvatarPath ?? "/images/drivers/default.jpg"
            });
            
            await _hubContext.Clients.Group("drivers").SendAsync("RideAssigned", new
            {
                rideId = ride.Id
            });
            
            await _hubContext.Clients.User(driverId).SendAsync("StartRideFlow", new
            {
                rideId = ride.Id,
                veteranName = ride.Veteran.FirstName + " " + ride.Veteran.LastName,
                startAddress = ride.StartAddress,
                endAddress = ride.EndAddress,
                startLat = ride.StartLatitude,
                startLng = ride.StartLongitude,
                endLat = ride.EndLatitude,
                endLng = ride.EndLongitude
            });
            
            return RedirectToAction("Index", "Driver");
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        try
        {
            await _taxiRepository.UpdateDriverLocationAsync(driverId, model.Latitude, model.Longitude);
            
            var activeRide = await _taxiRepository.GetActiveRideForDriverAsync(driverId);
            
            if (activeRide != null)
            {
                await _hubContext.Clients.Group(activeRide.Id.ToString()).SendAsync("DriverLocationUpdated", new
                {
                    RideId = activeRide.Id,
                    DriverLatitude = model.Latitude,
                    DriverLongitude = model.Longitude,
                    Progress = model.Progress
                });
            }
            
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> ArriveAtPickup(int rideId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ride = await _taxiRepository.GetRideByIdAsync(rideId);
    
        if (ride == null)
        {
            return Json(new { success = false, message = $"Поїздку з ID {rideId} не знайдено в базі даних" });
        }
    
        if (ride.DriverId != driverId)
        {
            return Json(new { 
                success = false, 
                message = $"Ви не є призначеним водієм для цієї поїздки. Призначений водій: {ride.DriverId}, ваш ID: {driverId}" 
            });
        }
    
        if (ride.Status != TaxiRideStatus.Accepted)
        {
            return Json(new { 
                success = false, 
                message = $"Неможливо прибути до місця: поїздка в неправильному статусі. Поточний статус: {ride.Status}" 
            });
        }
    
        try {
            await _taxiRepository.UpdateRideStatusAsync(rideId, TaxiRideStatus.DriverArriving);
        
            await _hubContext.Clients.Group(rideId.ToString()).SendAsync("RideStatusUpdated", new
            {
                RideId = rideId,
                Status = "DriverArrived",
                Message = "Водій прибув! Будь ласка, вийдіть до автомобіля."
            });
        
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Помилка при оновленні статусу: {ex.Message}" });
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> StartRide(int rideId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ride = await _taxiRepository.GetRideByIdAsync(rideId);
        
        if (ride == null || ride.DriverId != driverId)
        {
            return NotFound();
        }
        
        await _taxiRepository.UpdateRideStatusAsync(rideId, TaxiRideStatus.InProgress);
        await _taxiRepository.UpdateRidePickupTimeAsync(rideId, DateTime.Now);
        
        await _hubContext.Clients.Group(rideId.ToString()).SendAsync("RideStatusUpdated", new
        {
            RideId = rideId,
            Status = "InProgress",
            Message = "Поїздка розпочалася! Ви в дорозі"
        });
        
        return Json(new { success = true });
    }
    
    [HttpPost]
    public async Task<IActionResult> CompleteRide(int rideId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ride = await _taxiRepository.GetRideByIdAsync(rideId);
        
        if (ride == null || ride.DriverId != driverId)
        {
            return NotFound();
        }
        
        await _taxiRepository.UpdateRideStatusAsync(rideId, TaxiRideStatus.Completed);
        await _taxiRepository.UpdateRideCompleteTimeAsync(rideId, DateTime.Now);
        
        await _hubContext.Clients.Group(rideId.ToString()).SendAsync("RideStatusUpdated", new
        {
            RideId = rideId,
            Status = "Completed",
            Message = "Поїздку завершено! Дякуємо, що скористалися нашим сервісом"
        });
        
        return Json(new { success = true });
    }
    
    [HttpPost]
    public async Task<IActionResult> CancelRide(int rideId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ride = await _taxiRepository.GetRideByIdAsync(rideId);
    
        if (ride == null || ride.DriverId != driverId)
        {
            return NotFound();
        }
    
        if (ride.Status == TaxiRideStatus.InProgress)
        {
            return Json(new { 
                success = false, 
                message = "Не можна скасувати поїздку, яка вже розпочата" 
            });
        }
    
        if (ride.Status != TaxiRideStatus.Completed && ride.Status != TaxiRideStatus.Canceled)
        {
            await _taxiRepository.UpdateRideStatusAsync(rideId, TaxiRideStatus.Canceled);
        
            await _hubContext.Clients.Group(rideId.ToString()).SendAsync("RideStatusUpdated", new
            {
                RideId = rideId,
                Status = "Canceled",
                Message = "Поїздку скасовано водієм"
            });
        
            return Json(new { success = true });
        }
    
        return Json(new { 
            success = false, 
            message = "Не можна скасувати завершену чи вже скасовану поїздку" 
        });
    }
    
    [HttpGet]
    public async Task<IActionResult> RideDetails(int id)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ride = await _taxiRepository.GetRideByIdAsync(id);
    
        if (ride == null || (ride.DriverId != driverId && ride.Status != TaxiRideStatus.Requested))
        {
            return NotFound();
        }
    
        return View(ride);
    }
    
    [HttpGet]
    public async Task<IActionResult> CheckRideStatus(int rideId)
    {
        var ride = await _taxiRepository.GetRideByIdAsync(rideId);
        if (ride == null)
        {
            return NotFound();
        }

        return Json(new { status = ride.Status.ToString() });
    }

    [HttpGet]
    public async Task<IActionResult> GetScheduledRides()
    {
        try
        {
            var scheduledRides = await _taxiRepository.GetScheduledRidesAsync();
            var scheduledRidesData = scheduledRides.Select(ride => new
            {
                rideId = ride.Id,
                veteranName = ride.Veteran.FirstName + " " + ride.Veteran.LastName,
                startAddress = ride.StartAddress,
                endAddress = ride.EndAddress,
                startLat = ride.StartLatitude,
                startLng = ride.StartLongitude,
                endLat = ride.EndLatitude,
                endLng = ride.EndLongitude,
                distanceKm = ride.EstimatedDistance,
                estimatedPrice = 0,
                scheduledTime = ride.ScheduledTime?.ToString("dd.MM.yyyy HH:mm")
            }).ToList();
            
            return Json(new { success = true, rides = scheduledRidesData });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> AcceptScheduledRide(int rideId)
{
    var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    
    try
    {
        var driver = await _taxiRepository.GetDriverByIdAsync(driverId);
        if (driver == null)
        {
            return Json(new { success = false, message = "Водія не знайдено в системі" });
        }
        
        var ride = await _taxiRepository.GetRideByIdAsync(rideId);
        if (ride == null)
        {
            return Json(new { success = false, message = "Заплановану поїздку не знайдено" });
        }
        
        if (!ride.ScheduledTime.HasValue)
        {
            return Json(new { success = false, message = "Це не є запланованою поїздкою" });
        }

        if (!string.IsNullOrEmpty(ride.DriverId) && ride.DriverId != driverId)
        {
            return Json(new { success = false, message = "Ця поїздка вже прийнята іншим водієм" });
        }
        
        bool assignSuccess = await _taxiRepository.AssignDriverToScheduledRideAsync(rideId, driverId);
        
        if (!assignSuccess)
        {
            return Json(new { success = false, message = "Не вдалося прийняти заплановану поїздку" });
        }
        
        await _hubContext.Clients.Group("drivers").SendAsync("RideAssigned", new
        {
            rideId = ride.Id
        });
        
        await _hubContext.Clients.User(ride.VeteranId).SendAsync("ScheduledRideAccepted", new
        {
            rideId = ride.Id,
            driverName = $"{driver.FirstName} {driver.LastName}",
            driverPhone = driver.PhoneNumber,
            carModel = driver.CarModel,
            licensePlate = driver.LicensePlate,
            driverPhoto = driver.AvatarPath ?? "/images/drivers/default.jpg",
            scheduledTime = ride.ScheduledTime?.ToString("dd.MM.yyyy HH:mm")
        });
        
        return Json(new { 
            success = true,
            scheduledTime = ride.ScheduledTime?.ToString("dd.MM.yyyy HH:mm"),
            rideDetails = new {
                rideId = ride.Id,
                startAddress = ride.StartAddress,
                endAddress = ride.EndAddress,
                veteranName = $"{ride.Veteran?.FirstName} {ride.Veteran?.LastName}"
            }
        });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}

    [HttpGet]
    public async Task<IActionResult> GetActiveScheduledRides()
    {
        try
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var activeScheduledRides = await _taxiRepository.GetActiveScheduledRidesAsync();
            
            var filteredRides = activeScheduledRides
                .Where(ride => string.IsNullOrEmpty(ride.DriverId) || ride.DriverId == driverId)
                .ToList();
            
            var activeScheduledRidesData = filteredRides.Select(ride => new
            {
                rideId = ride.Id,
                veteranName = ride.Veteran.FirstName + " " + ride.Veteran.LastName,
                startAddress = ride.StartAddress,
                endAddress = ride.EndAddress,
                startLat = ride.StartLatitude,
                startLng = ride.StartLongitude,
                endLat = ride.EndLatitude,
                endLng = ride.EndLongitude,
                distanceKm = ride.EstimatedDistance,
                estimatedPrice = 0,
                scheduledTime = ride.ScheduledTime?.ToString("dd.MM.yyyy HH:mm"),
                driverAssigned = !string.IsNullOrEmpty(ride.DriverId) && ride.DriverId == driverId
            }).ToList();
            
            return Json(new { success = true, rides = activeScheduledRidesData });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка при отриманні активних запланованих поїздок: {ex.Message}");
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> GetDriverScheduledRides()
    {
        try
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var scheduledRides = await _taxiRepository.GetDriverAcceptedScheduledRidesAsync(driverId);
        
            var scheduledRidesData = scheduledRides.Select(ride => new
            {
                rideId = ride.Id,
                veteranName = ride.Veteran.FirstName + " " + ride.Veteran.LastName,
                startAddress = ride.StartAddress,
                endAddress = ride.EndAddress,
                startLat = ride.StartLatitude,
                startLng = ride.StartLongitude,
                endLat = ride.EndLatitude,
                endLng = ride.EndLongitude,
                distanceKm = ride.EstimatedDistance,
                estimatedPrice = 0,
                scheduledTime = ride.ScheduledTime?.ToString("dd.MM.yyyy HH:mm"),
                driverAssigned = true
            }).ToList();
        
            return Json(new { success = true, rides = scheduledRidesData });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> StartScheduledRide(int rideId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        try
        {
            var ride = await _taxiRepository.GetRideByIdAsync(rideId);
            if (ride == null)
            {
                return Json(new { success = false, message = "Поїздку не знайдено" });
            }
            
            if (ride.DriverId != driverId)
            {
                return Json(new { success = false, message = "Цю поїздку призначено іншому водію" });
            }
            
            if (ride.Status != TaxiRideStatus.Requested && ride.Status != TaxiRideStatus.Accepted)
            {
                return Json(new { success = false, message = "Неможливо розпочати поїздку в поточному статусі" });
            }
            
            await _taxiRepository.UpdateRideStatusAsync(rideId, TaxiRideStatus.Accepted);
            
            var driver = await _taxiRepository.GetDriverByIdAsync(driverId);
            
            await _hubContext.Clients.User(ride.VeteranId).SendAsync("RideAccepted", new
            {
                rideId = ride.Id,
                driverName = $"{driver.FirstName} {driver.LastName}",
                driverPhone = driver.PhoneNumber,
                carModel = driver.CarModel,
                licensePlate = driver.LicensePlate,
                driverPhoto = driver.AvatarPath ?? "/images/drivers/default.jpg"
            });
            
            return Json(new { 
                success = true, 
                ride = new {
                    id = ride.Id,
                    veteranName = $"{ride.Veteran?.FirstName} {ride.Veteran?.LastName}",
                    startAddress = ride.StartAddress,
                    endAddress = ride.EndAddress,
                    startLat = ride.StartLatitude,
                    startLng = ride.StartLongitude,
                    endLat = ride.EndLatitude,
                    endLng = ride.EndLongitude,
                    distanceKm = ride.EstimatedDistance
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}

public class LocationUpdateModel
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Progress { get; set; }
}