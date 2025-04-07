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

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            IConsultationRepository consultationRepository,
            IEventRepository eventRepository,
            ILogger<ProfileController> logger, 
            IWebHostEnvironment webHostEnvironment, 
            INewsRepository newsRepository,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _consultationRepository = consultationRepository;
            _eventRepository = eventRepository;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _newsRepository = newsRepository;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
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
            if (avatarFile == null || avatarFile.Length == 0)
            {
                TempData["Error"] = "Файл не вибрано";
                return RedirectToAction("Index");
            }   

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(avatarFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
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

                if (result.Succeeded)
                {
                    TempData["Success"] = "Аватар успішно оновлено";
                }
                else
                {
                    TempData["Error"] = "Помилка при оновленні профілю";
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Помилка: {ex.Message}";
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
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

    // Оновлюємо email, якщо він змінився
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
    }
}
