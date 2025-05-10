using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using veterans_site.Hubs;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.Services;
using veterans_site.ViewModels;

namespace veterans_site.Controllers;

[Authorize(Roles = "Veteran")]
public class SocialTaxiController : Controller
{
    private readonly ISocialTaxiRepository _taxiRepository;
    private readonly UberApiService _uberApiService;
    private readonly IHubContext<TaxiHub> _taxiHubContext;
    private readonly IEmailService _emailService;
    
    public SocialTaxiController(
        ISocialTaxiRepository taxiRepository,
        UberApiService uberApiService,
        IHubContext<TaxiHub> taxiHubContext,
        IEmailService emailService)
    {
        _taxiRepository = taxiRepository;
        _uberApiService = uberApiService;
        _taxiHubContext = taxiHubContext;
        _emailService = emailService;
    }
    
    public IActionResult Index()
    {
        return View();
    }
    
    [HttpGet]
    public async Task<IActionResult> MyRides(int? page)
    {
        var veteranId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int pageSize = 10;
        int pageNumber = page ?? 1;
    
        var allRides = await _taxiRepository.GetRidesForVeteranAsync(veteranId);
    
        var rides = PaginatedList<TaxiRide>.Create(allRides, pageNumber, pageSize);
    
        return View(rides);
    }
    
    [HttpPost]
    public async Task<IActionResult> GetEstimate([FromBody] EstimateRequestViewModel request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        try
        {
            var estimate = await _uberApiService.GetPriceEstimateAsync(
                request.StartLatitude, 
                request.StartLongitude, 
                request.EndLatitude, 
                request.EndLongitude,
                request.DistanceKm,
                request.DurationMinutes);
                
            return Json(new
            {
                success = true,
                estimatedPrice = estimate.EstimatedPrice,
                estimatedDurationMinutes = request.DurationMinutes ?? Math.Round(estimate.EstimatedDuration / 60.0, 1),
                distanceKm = request.DistanceKm ?? estimate.Distance
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RequestRide([FromBody] TaxiRideViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            Console.WriteLine("Validation errors: " + errors);
            return BadRequest(new { errors = errors });
        }
        
        if (model.ScheduledTime.HasValue && model.ScheduledTime.Value <= DateTime.Now)
        {
            return BadRequest(new { errors = "Запланований час повинен бути в майбутньому" });
        }

        var veteranId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var veteran = await _taxiRepository.GetVeteranByIdAsync(veteranId);

        if (veteran == null)
        {
            return NotFound(new { errors = "Користувача не знайдено" });
        }

        var ride = new TaxiRide
        {
            VeteranId = veteranId,
            StartAddress = model.StartAddress,
            EndAddress = model.EndAddress,
            StartLatitude = model.StartLatitude,
            StartLongitude = model.StartLongitude,
            EndLatitude = model.EndLatitude,
            EndLongitude = model.EndLongitude,
            EstimatedDuration = model.EstimatedDuration,
            EstimatedDistance = model.EstimatedDistance,
            Status = TaxiRideStatus.Requested,
            RequestTime = DateTime.Now,
            ScheduledTime = model.ScheduledTime,
            CarTypes = model.CarTypes
        };

        var createdRide = await _taxiRepository.CreateRideAsync(ride);

        try
        {
            var eligibleDriverIds = await _taxiRepository.GetDriverIdsByCarTypesAsync(model.CarTypes ?? new List<string>());
            var availableDrivers = await _taxiRepository.GetAvailableDriversAsync();
            var eligibleAvailableDrivers = availableDrivers.Where(d => eligibleDriverIds.Contains(d.Id)).ToList();
            
            if (ride.ScheduledTime.HasValue)
            {
                foreach (var driverId in eligibleDriverIds)
                {
                    await _taxiHubContext.Clients.User(driverId).SendAsync("NotifyNewScheduledRide", new
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
                        scheduledTime = ride.ScheduledTime.Value.ToString("dd.MM.yyyy HH:mm"),
                        carTypes = ride.CarTypes
                    });
                }
            }

            foreach (var driverId in eligibleDriverIds)
            {
                await _taxiHubContext.Clients.User(driverId).SendAsync("NewRideRequest", new
                {
                    rideId = createdRide.Id,
                    veteranName = veteran.FirstName + " " + veteran.LastName,
                    startAddress = createdRide.StartAddress,
                    endAddress = createdRide.EndAddress,
                    startLat = createdRide.StartLatitude,
                    startLng = createdRide.StartLongitude,
                    endLat = createdRide.EndLatitude,
                    endLng = createdRide.EndLongitude,
                    distanceKm = createdRide.EstimatedDistance,
                    estimatedPrice = 0,
                    isScheduled = createdRide.ScheduledTime.HasValue,
                    scheduledTime = createdRide.ScheduledTime?.ToString("dd.MM.yyyy HH:mm"),
                    carTypes = createdRide.CarTypes
                });
            }

            foreach (var driver in eligibleAvailableDrivers)
            {
                if (!string.IsNullOrEmpty(driver.Email))
                {
                    var emailSubject = "Нове замовлення таксі";
                    var emailBody = 
                        "<h2>Нове замовлення таксі</h2>" +
                        "<p>Шановний водій,</p>" +
                        "<p>Надійшло нове замовлення на поїздку:</p>" +
                        "<ul>" +
                        $"<li>Пасажир: {veteran.FirstName} {veteran.LastName}</li>" +
                        $"<li>Звідки: {createdRide.StartAddress}</li>" +
                        $"<li>Куди: {createdRide.EndAddress}</li>" +
                        $"<li>Відстань: {createdRide.EstimatedDistance} км</li>" +
                        $"<li>Орієнтовна вартість: 0 грн</li>";
                    
                    if (createdRide.CarTypes != null && createdRide.CarTypes.Any())
                    {
                        emailBody += $"<li>Типи автомобілів: {string.Join(", ", createdRide.CarTypes)}</li>";
                    }
                    
                    if (createdRide.ScheduledTime.HasValue)
                    {
                        emailBody += $"<li>Запланований час: {createdRide.ScheduledTime.Value.ToString("dd.MM.yyyy HH:mm")}</li>";
                    }
                    else
                    {
                        emailBody += "<li>Час: Зараз</li>";
                    }
                    
                    emailBody +=
                        "</ul>" +
                        "<p>Щоб прийняти замовлення, будь ласка, увійдіть у свій кабінет.</p>" +
                        "<p>З повагою,<br>Команда Соціального Таксі</p>";
                    
                    await _emailService.SendEmailAsync(driver.Email, emailSubject, emailBody);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка при обробці сповіщень: {ex.Message}");
        }

        return Json(new { success = true, rideId = createdRide.Id });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetRideStatus(int rideId)
    {
        var veteranId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ride = await _taxiRepository.GetRideByIdAsync(rideId);
        
        if (ride == null || ride.VeteranId != veteranId)
        {
            return NotFound();
        }
        
        var driver = ride.Driver;
        
        return Json(new
        {
            id = ride.Id,
            status = ride.Status.ToString(),
            driverName = driver?.FirstName + " " + driver?.LastName,
            driverPhone = driver?.PhoneNumber,
            carModel = driver?.CarModel,
            licensePlate = driver?.LicensePlate,
            driverLatitude = driver?.CurrentLatitude,
            driverLongitude = driver?.CurrentLongitude,
            requestTime = ride.RequestTime,
            acceptTime = ride.AcceptTime,
            pickupTime = ride.PickupTime,
            completeTime = ride.CompleteTime,
            scheduledTime = ride.ScheduledTime
        });
    }
    
    [HttpPost]
    public async Task<IActionResult> CancelRide(int rideId)
    {
        var veteranId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ride = await _taxiRepository.GetRideByIdAsync(rideId);
    
        if (ride == null || ride.VeteranId != veteranId)
        {
            return NotFound();
        }
    
        if (ride.Status != TaxiRideStatus.Completed && ride.Status != TaxiRideStatus.Canceled)
        {
            var previousStatus = ride.Status;
            await _taxiRepository.UpdateRideStatusAsync(rideId, TaxiRideStatus.Canceled);
        
            await _taxiHubContext.Clients.Group("drivers").SendAsync("RideCanceled", new
            {
                rideId = ride.Id,
                message = "Поїздку скасовано пасажиром",
                status = "Canceled",
                wasSearching = previousStatus == TaxiRideStatus.Requested
            });

            if (!string.IsNullOrEmpty(ride.DriverId))
            {
                await _taxiHubContext.Clients.User(ride.DriverId).SendAsync("RideCanceled", new
                {
                    rideId = ride.Id,
                    message = "Поїздку скасовано пасажиром",
                    status = "Canceled",
                    wasSearching = previousStatus == TaxiRideStatus.Requested
                });
            }
        
            return Json(new { success = true });
        }
    
        return Json(new { success = false, message = "Не можна скасувати завершену чи вже скасовану поїздку" });
    }
    
    [HttpGet]
    public async Task<IActionResult> RideDetails(int id)
    {
        var veteranId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ride = await _taxiRepository.GetRideByIdAsync(id);
    
        if (ride == null || ride.VeteranId != veteranId)
        {
            return NotFound();
        }
    
        return View(ride);
    }

    [HttpPost]
    public async Task<IActionResult> ScheduleRide([FromBody] TaxiRideViewModel model)
    {
        if (!ModelState.IsValid || !model.ScheduledTime.HasValue)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return BadRequest(new { errors = errors });
        }

        if (model.ScheduledTime.Value <= DateTime.Now)
        {
            return BadRequest(new { errors = "Запланований час повинен бути в майбутньому" });
        }

        var veteranId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var veteran = await _taxiRepository.GetVeteranByIdAsync(veteranId);

        if (veteran == null)
        {
            return NotFound(new { errors = "Користувача не знайдено" });
        }

        var ride = new TaxiRide
        {
            VeteranId = veteranId,
            StartAddress = model.StartAddress,
            EndAddress = model.EndAddress,
            StartLatitude = model.StartLatitude,
            StartLongitude = model.StartLongitude,
            EndLatitude = model.EndLatitude,
            EndLongitude = model.EndLongitude,
            EstimatedDuration = model.EstimatedDuration,
            EstimatedDistance = model.EstimatedDistance,
            Status = TaxiRideStatus.Requested,
            RequestTime = DateTime.Now,
            ScheduledTime = model.ScheduledTime,
            CarTypes = model.CarTypes
        };

        var createdRide = await _taxiRepository.CreateRideAsync(ride);

        try
        {
            var eligibleDriverIds = await _taxiRepository.GetDriverIdsByCarTypesAsync(model.CarTypes ?? new List<string>());
            
            foreach (var driverId in eligibleDriverIds)
            {
                await _taxiHubContext.Clients.User(driverId).SendAsync("NewScheduledRideRequest", new
                {
                    rideId = createdRide.Id,
                    veteranName = veteran.FirstName + " " + veteran.LastName,
                    startAddress = createdRide.StartAddress,
                    endAddress = createdRide.EndAddress,
                    startLat = createdRide.StartLatitude,
                    startLng = createdRide.StartLongitude,
                    endLat = createdRide.EndLatitude,
                    endLng = createdRide.EndLongitude,
                    distanceKm = createdRide.EstimatedDistance,
                    estimatedPrice = 0,
                    scheduledTime = createdRide.ScheduledTime?.ToString("dd.MM.yyyy HH:mm"),
                    carTypes = createdRide.CarTypes
                });
            }

            var allDrivers = await _taxiRepository.GetAllDriversAsync();
            var eligibleDrivers = allDrivers.Where(d => eligibleDriverIds.Contains(d.Id)).ToList();
            
            foreach (var driver in eligibleDrivers)
            {
                if (!string.IsNullOrEmpty(driver.Email))
                {
                    var emailSubject = "Нова запланована поїздка";
                    var emailBody = 
                        "<h2>Нова запланована поїздка</h2>" +
                        "<p>Шановний водій,</p>" +
                        "<p>Надійшло нове замовлення на заплановану поїздку:</p>" +
                        "<ul>" +
                        $"<li>Пасажир: {veteran.FirstName} {veteran.LastName}</li>" +
                        $"<li>Звідки: {createdRide.StartAddress}</li>" +
                        $"<li>Куди: {createdRide.EndAddress}</li>" +
                        $"<li>Відстань: {createdRide.EstimatedDistance} км</li>" +
                        $"<li>Орієнтовна вартість: 0 грн</li>";
                    
                    if (createdRide.CarTypes != null && createdRide.CarTypes.Any())
                    {
                        emailBody += $"<li>Типи автомобілів: {string.Join(", ", createdRide.CarTypes)}</li>";
                    }
                    
                    emailBody +=
                        $"<li>Запланований час: {createdRide.ScheduledTime.Value.ToString("dd.MM.yyyy HH:mm")}</li>" +
                        "</ul>" +
                        "<p>Щоб прийняти замовлення, будь ласка, увійдіть у свій кабінет.</p>" +
                        "<p>З повагою,<br>Команда Соціального Таксі</p>";
                    
                    await _emailService.SendEmailAsync(driver.Email, emailSubject, emailBody);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка при обробці сповіщень: {ex.Message}");
        }

        return Json(new { success = true, rideId = createdRide.Id });
    }
}