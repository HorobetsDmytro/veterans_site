using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.ViewModels;

namespace veterans_site.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConsultationRepository _consultationRepository;
        private readonly IEventRepository _eventRepository;
        private readonly INewsRepository _newsRepository;
        private readonly ILogger<ProfileController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private SignInManager<ApplicationUser> _signInManager;
        private readonly ISocialTaxiRepository _taxiRepository;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            IConsultationRepository consultationRepository,
            IEventRepository eventRepository,
            ILogger<ProfileController> logger, 
            IWebHostEnvironment webHostEnvironment, 
            INewsRepository newsRepository,
            SignInManager<ApplicationUser> signInManager, 
            ISocialTaxiRepository taxiRepository)
        {
            _userManager = userManager;
            _consultationRepository = consultationRepository;
            _eventRepository = eventRepository;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _newsRepository = newsRepository;
            _signInManager = signInManager;
            _taxiRepository = taxiRepository;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.AjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var userConsultations = await _consultationRepository.GetUserConsultationsWithSlotsAsync(user.Id);
            var currentDate = DateTime.Now;

            var upcomingConsultations = userConsultations
                .Where(c => c.DateTime > currentDate && c.Status != ConsultationStatus.Cancelled)
                .OrderBy(c => c.DateTime);

            var pastConsultations = userConsultations
                .Where(c => c.DateTime <= currentDate || c.Status == ConsultationStatus.Cancelled)
                .OrderByDescending(c => c.DateTime);

            var userEvents = await _eventRepository.GetUserEventsAsync(user.Id);

            var upcomingEvents = userEvents
                .Where(e => e.Date > currentDate && e.Status != EventStatus.Cancelled)
                .OrderBy(e => e.Date);

            var pastEvents = userEvents
                .Where(e => e.Date <= currentDate || e.Status == EventStatus.Cancelled)
                .OrderByDescending(e => e.Date);

            var viewModel = new UserProfileViewModel
            {
                User = user,
                UpcomingConsultations = upcomingConsultations,
                PastConsultations = pastConsultations,
                UpcomingEvents = upcomingEvents,
                PastEvents = pastEvents,
                TotalConsultations = userConsultations.Count(),
                TotalEvents = userEvents.Count()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> ConsultationHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var consultations = await _consultationRepository.GetUserConsultationsAsync(user.Id);
            return View(consultations.OrderByDescending(c => c.DateTime));
        }

        public async Task<IActionResult> EventHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var events = await _eventRepository.GetUserEventsAsync(user.Id);
            return View(events.OrderByDescending(e => e.Date));
        }

        [HttpPost]
        public async Task<IActionResult> CancelConsultation(int consultationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var consultation = await _consultationRepository.GetByIdAsync(consultationId);
            if (consultation == null || consultation.UserId != user.Id)
            {
                return NotFound();
            }

            if (consultation.DateTime <= DateTime.Now)
            {
                TempData["Error"] = "Не можна скасувати консультацію, яка вже відбулась або триває.";
                return RedirectToAction(nameof(Index));
            }

            if (consultation.Format == ConsultationFormat.Group)
            {
                consultation.BookedParticipants--;
            }
            consultation.Status = ConsultationStatus.Cancelled;
            consultation.UserId = null;
            consultation.IsBooked = false;

            await _consultationRepository.UpdateAsync(consultation);

            TempData["Success"] = "Консультацію успішно скасовано.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEvent(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                if (!await _eventRepository.IsUserRegisteredForEventAsync(id, userId))
                {
                    TempData["Error"] = "Ви не зареєстровані на цю подію.";
                    return RedirectToAction("Index", "Profile");
                }

                await _eventRepository.CancelEventParticipationAsync(id, userId);

                TempData["Success"] = "Реєстрацію на подію скасовано.";
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Cancel method: {ex.Message}");
                TempData["Error"] = "Виникла помилка при скасуванні реєстрації.";
                return RedirectToAction("Index", "Profile");
            }
        }

        public async Task<IActionResult> ConsultationDetails(int id)
        {
            var userId = _userManager.GetUserId(User);
            var consultation = await _consultationRepository.GetByIdWithSlotsAsync(id);

            if (consultation == null)
            {
                return NotFound();
            }

            if (!await _consultationRepository.IsUserBookedForConsultationAsync(id, userId))
            {
                return Forbid();
            }

            ViewBag.UserId = userId;
            return View(consultation);
        }

        public async Task<IActionResult> EventDetails(int id)
        {
            var userId = _userManager.GetUserId(User);
            var evt = await _eventRepository.GetByIdWithParticipantsAsync(id);

            if (evt == null)
            {
                return NotFound();
            }

            if (!evt.EventParticipants.Any(p => p.UserId == userId))
            {
                return Forbid();
            }

            return View(evt);
        }
        
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            if (avatarFile == null || avatarFile.Length == 0)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = "Файл не вибрано" });
                }
                
                TempData["Error"] = "Файл не вибрано";
                return RedirectToAction("Index");
            }   

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(avatarFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = "Дозволені лише зображення (JPG, JPEG, PNG, GIF)" });
                }
                
                TempData["Error"] = "Дозволені лише зображення (JPG, JPEG, PNG, GIF)";
                return RedirectToAction("Index");
            }

            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
            
            Directory.CreateDirectory(uploadsFolder);
            
            var filePath = Path.Combine(uploadsFolder, fileName);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(fileStream);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);
                
                if (!string.IsNullOrEmpty(user.AvatarPath))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                user.AvatarPath = $"/uploads/avatars/{fileName}";
                var result = await _userManager.UpdateAsync(user);
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = $"Помилка: {ex.Message}" });
                }
                
            }

            return RedirectToAction("Index");
        }
        
        [HttpGet]
        public async Task<IActionResult> EditAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditAccountViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccount(EditAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
                await _userManager.SetUserNameAsync(user, model.Email);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Ваші дані успішно оновлено";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ChangePasswordViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, 
                model.CurrentPassword, model.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Ваш пароль успішно змінено";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet]
        public async Task<IActionResult> GetDriverStatistics()
        {
            try
            {
                var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                var allRides = await _taxiRepository.GetRidesForDriverAsync(driverId);
                
                var completedRides = allRides.Where(r => r.Status == TaxiRideStatus.Completed).ToList();
                var canceledRides = allRides.Where(r => r.Status == TaxiRideStatus.Canceled).ToList();
                var inProgressRides = allRides.Where(r => 
                    r.Status == TaxiRideStatus.Accepted || 
                    r.Status == TaxiRideStatus.DriverArriving || 
                    r.Status == TaxiRideStatus.InProgress).ToList();
                
                double totalDistance = completedRides.Sum(r => r.EstimatedDistance);
                
                var scheduledRides = await _taxiRepository.GetActiveScheduledRidesForDriverAsync(driverId);
                
                var today = DateTime.Now.Date;
                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => today.AddDays(-i))
                    .Reverse()
                    .ToList();
                
                var ridesByDay = last7Days.Select(date => {
                    var count = completedRides.Count(r => 
                        r.CompleteTime.HasValue && 
                        r.CompleteTime.Value.Date == date);
                    return new { Date = date, Count = count };
                }).ToList();
                
                var popularRoutes = completedRides
                    .GroupBy(r => new { Start = r.StartAddress, End = r.EndAddress })
                    .Select(g => new {
                        ShortName = $"{g.Key.Start.Split(',')[0].Trim()} - {g.Key.End.Split(',')[0].Trim()}",
                        FullStart = g.Key.Start,
                        FullEnd = g.Key.End,
                        Count = g.Count() 
                    })
                    .OrderByDescending(g => g.Count)
                    .Take(5)
                    .ToList();
                    
                var activityByHour = completedRides
                    .Where(r => r.CompleteTime.HasValue)
                    .GroupBy(r => r.CompleteTime.Value.Hour)
                    .Select(g => new { 
                        Hour = g.Key, 
                        Count = g.Count() 
                    })
                    .OrderBy(g => g.Hour)
                    .ToList();
                    
                var hoursLabels = new List<string>();
                var hoursData = new List<int>();
                
                for (int i = 6; i <= 22; i += 2)
                {
                    hoursLabels.Add($"{i}:00");
                    hoursData.Add(activityByHour.FirstOrDefault(a => a.Hour == i)?.Count ?? 0);
                }
                
                var averageDurationByDay = Enumerable.Range(0, 7)
                    .Select(i => {
                        var dayOfWeek = (int)DateTime.Now.AddDays(-i).DayOfWeek;
                        var ridesOnThisDay = completedRides
                            .Where(r => r.CompleteTime.HasValue && r.PickupTime.HasValue && 
                                   (int)r.CompleteTime.Value.DayOfWeek == dayOfWeek);
                        
                        double avgDuration = ridesOnThisDay.Any() 
                            ? ridesOnThisDay.Average(r => 
                                (r.CompleteTime.Value - r.PickupTime.Value).TotalMinutes) 
                            : 0;
                        
                        return new { DayOfWeek = dayOfWeek, AvgDuration = avgDuration };
                    })
                    .OrderBy(x => x.DayOfWeek)
                    .ToList();
                
                var ridesByCarType = new Dictionary<string, int>();
                foreach (var ride in completedRides.Where(r => r.CarTypes != null && r.CarTypes.Any()))
                {
                    foreach (var carType in ride.CarTypes)
                    {
                        if (ridesByCarType.ContainsKey(carType))
                            ridesByCarType[carType]++;
                        else
                            ridesByCarType[carType] = 1;
                    }
                }
                
                var ridesByTimeOfDay = new Dictionary<string, int>
                {
                    { "Ранок (6-10)", completedRides.Count(r => r.PickupTime.HasValue && r.PickupTime.Value.Hour >= 6 && r.PickupTime.Value.Hour < 10) },
                    { "День (10-16)", completedRides.Count(r => r.PickupTime.HasValue && r.PickupTime.Value.Hour >= 10 && r.PickupTime.Value.Hour < 16) },
                    { "Вечір (16-21)", completedRides.Count(r => r.PickupTime.HasValue && r.PickupTime.Value.Hour >= 16 && r.PickupTime.Value.Hour < 21) },
                    { "Ніч (21-6)", completedRides.Count(r => r.PickupTime.HasValue && (r.PickupTime.Value.Hour >= 21 || r.PickupTime.Value.Hour < 6)) }
                };
                
                var averageDistanceByDay = Enumerable.Range(0, 7)
                    .Select(i => {
                        var dayOfWeek = (int)DateTime.Now.AddDays(-i).DayOfWeek;
                        var ridesOnThisDay = completedRides
                            .Where(r => r.CompleteTime.HasValue && 
                                   (int)r.CompleteTime.Value.DayOfWeek == dayOfWeek);
                        
                        double avgDistance = ridesOnThisDay.Any() 
                            ? ridesOnThisDay.Average(r => r.EstimatedDistance) 
                            : 0;
                        
                        return new { DayOfWeek = dayOfWeek, AvgDistance = avgDistance };
                    })
                    .OrderBy(x => x.DayOfWeek)
                    .ToList();
                    
                int scheduledRidesCount = completedRides.Count(r => r.ScheduledTime.HasValue);
                int spontaneousRidesCount = completedRides.Count(r => !r.ScheduledTime.HasValue);
                
                string[] daysOfWeek = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд" };
                
                var plannedVsRegular = new
                {
                    labels = new[] { "Заплановані", "Звичайні" },
                    data = new[] { 
                        completedRides.Count(r => r.ScheduledTime.HasValue),
                        completedRides.Count(r => !r.ScheduledTime.HasValue)
                    }
                };
                
                var statistics = new
                {
                    totalRides = completedRides.Count,
                    totalDistance,
                    scheduledRides = scheduledRides.Count,
                    ridesByDay = new
                    {
                        labels = last7Days.Select(d => d.ToString("ddd")).ToArray(),
                        data = ridesByDay.Select(r => r.Count).ToArray()
                    },
                    rideStatuses = new int[] 
                    { 
                        completedRides.Count, 
                        canceledRides.Count, 
                        inProgressRides.Count 
                    },
                    popularRoutes = popularRoutes.Select(r => new { 
                        shortName = r.ShortName, 
                        fullStart = r.FullStart, 
                        fullEnd = r.FullEnd, 
                        count = r.Count 
                    }).ToArray(),
                    activityHours = new
                    {
                        labels = hoursLabels.ToArray(),
                        data = hoursData.ToArray()
                    },
                    rideTypes = new
                    {
                        labels = ridesByCarType.Keys.ToArray(),
                        data = ridesByCarType.Values.ToArray()
                    },
                    averageDistanceByDay = new
                    {
                        labels = daysOfWeek,
                        data = new double[7].Select((_, i) => 
                            averageDistanceByDay.FirstOrDefault(d => d.DayOfWeek == i)?.AvgDistance ?? 0).ToArray()
                    },
                    averageDurationByDay = new
                    {
                        labels = daysOfWeek,
                        data = new double[7].Select((_, i) => 
                            averageDurationByDay.FirstOrDefault(d => d.DayOfWeek == i)?.AvgDuration ?? 0).ToArray()
                    },
                    ridesByTimeOfDay = new
                    {
                        labels = ridesByTimeOfDay.Keys.ToArray(),
                        data = ridesByTimeOfDay.Values.ToArray()
                    },
                    ridePlanningType = new
                    {
                        labels = new[] { "Заплановані", "Звичайні" },
                        data = new[] { scheduledRidesCount, spontaneousRidesCount }
                    }
                };
                
                return Json(statistics);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCarTypes(List<string> CarTypes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.CarType = CarTypes != null ? string.Join(",", CarTypes) : null;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Типи автомобіля успішно оновлені";
            }
            else
            {
                TempData["Error"] = "Помилка при оновленні типів автомобіля";
            }

            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Не вдалося завантажити користувача з ID '{_userManager.GetUserId(User)}'.");
            }

            _logger.LogInformation("Користувач {UserId} запитав видалення свого акаунту.", user.Id);

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                
                TempData["Error"] = "Не вдалося видалити акаунт. Спробуйте пізніше або зверніться до адміністратора.";

                return RedirectToAction("Index", "Profile");
            }

            await _signInManager.SignOutAsync();
            
            _logger.LogInformation("Користувач з ID {UserId} видалив свій акаунт.", user.Id);
            
            TempData["Success"] = "Акаунт видалено\", \"Ваш акаунт та всі пов'язані дані були успішно видалені.";

            return RedirectToAction("Index", "Home");
        }
    }
}
