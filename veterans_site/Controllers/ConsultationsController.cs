using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Extensions;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.Repositories;
using veterans_site.Services;
using veterans_site.ViewModels;

namespace veterans_site.Controllers
{
    public class ConsultationsController : Controller
    {
        private readonly IConsultationRepository _consultationRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<ConsultationsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly VeteranSupportDBContext _context;
        private const int PageSize = 6;

        public ConsultationsController(
            IConsultationRepository consultationRepository,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<ConsultationsController> logger,
            VeteranSupportDBContext context)
        {
            _consultationRepository = consultationRepository;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(
            ConsultationType? type = null,
            ConsultationFormat? format = null,
            double? minPrice = null,
            double? maxPrice = null,
            string sortOrder = null,
            int page = 1)
        {
            var consultations = await _consultationRepository.GetAvailableConsultationsAsync(
                type, format, minPrice, maxPrice, sortOrder, page, PageSize);

            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                foreach (var consultation in consultations)
                {
                    consultation.IsBooked = await _consultationRepository.IsUserBookedForConsultationAsync(consultation.Id, userId);
                }
            }

            //var totalPages = await _consultationRepository.GetTotalPagesAsync(
            //    type, format, null, minPrice, maxPrice, PageSize, true);

            var viewModel = new PublicConsultationIndexViewModel
            {
                Consultations = consultations,
                CurrentType = type,
                CurrentFormat = format,
                CurrentSort = sortOrder,
                CurrentPage = page,
                //TotalPages = totalPages,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Book(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!user.IsActive)
            {
                TempData["Error"] = "Ваш обліковий запис неактивний. Ви не можете записуватись на консультації.";
                return RedirectToAction("Index");
            }

            var consultation = await _consultationRepository.GetByIdWithSlotsAsync(id);
            if (consultation == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (await _consultationRepository.IsUserBookedForConsultationAsync(id, userId))
            {
                TempData["Error"] = "Ви вже записані на цю консультацію.";
                return RedirectToAction(nameof(Index));
            }

            if (consultation.Format == ConsultationFormat.Individual)
            {
                // Перевіряємо чи є доступні слоти
                if (!consultation.Slots.Any(s => !s.IsBooked))
                {
                    TempData["Error"] = "На жаль, всі слоти вже заброньовані.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (consultation.Format == ConsultationFormat.Group &&
                     consultation.BookedParticipants >= consultation.MaxParticipants)
            {
                TempData["Error"] = "На жаль, вже немає вільних місць.";
                return RedirectToAction(nameof(Index));
            }

            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookConfirm(int id, int? slotId = null)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                var existingRequest = await _context.ConsultationBookingRequests
                    .AnyAsync(r => r.ConsultationId == id &&
                                  r.UserId == userId &&
                                  (r.IsApproved == null || r.IsApproved == true) &&
                                  r.ExpiryTime > DateTime.UtcNow);

                if (existingRequest)
                {
                    TempData["Error"] = "Ви вже маєте активний запит на цю консультацію.";
                    return RedirectToAction(nameof(Index));
                }

                if (await _consultationRepository.IsUserBookedForConsultationAsync(id, userId))
                {
                    TempData["Error"] = "Ви вже записані на цю консультацію.";
                    return RedirectToAction(nameof(Index));
                }

                var specialist = await _userManager.GetUsersInRoleAsync("Specialist");
                var consultation = await _context.Consultations
                    .Include(c => c.Slots)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (consultation == null)
                {
                    TempData["Error"] = "Консультацію не знайдено.";
                    return RedirectToAction(nameof(Index));
                }

                var specialistUser = specialist.FirstOrDefault(s =>
                    $"{s.FirstName} {s.LastName}" == consultation.SpecialistName);

                if (specialistUser == null)
                {
                    TempData["Error"] = "Не вдалося знайти спеціаліста.";
                    return RedirectToAction(nameof(Index));
                }

                if (consultation.Format == ConsultationFormat.Individual)
                {
                    if (!slotId.HasValue)
                    {
                        TempData["Error"] = "Необхідно вибрати слот для запису.";
                        return RedirectToAction(nameof(Book), new { id });
                    }

                    var slot = await _context.ConsultationSlots
                        .FirstOrDefaultAsync(s => s.Id == slotId && s.ConsultationId == id);

                    if (slot == null || slot.IsBooked)
                    {
                        TempData["Error"] = "Вибраний слот недоступний.";
                        return RedirectToAction(nameof(Book), new { id });
                    }
                }
                else if (consultation.BookedParticipants >= consultation.MaxParticipants)
                {
                    TempData["Error"] = "На жаль, всі місця вже заброньовані.";
                    return RedirectToAction(nameof(Index));
                }

                var token = Guid.NewGuid().ToString();
                var bookingRequest = new ConsultationBookingRequest
                {
                    ConsultationId = id,
                    SlotId = slotId,
                    UserId = userId,
                    RequestTime = DateTime.UtcNow,
                    Token = token,
                    ExpiryTime = DateTime.UtcNow.AddDays(1)
                };

                _context.ConsultationBookingRequests.Add(bookingRequest);
                await _context.SaveChangesAsync();

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var confirmLink = $"{baseUrl}/Specialist/Consultation/ConfirmBooking?token={token}&confirm=true";
                var rejectLink = $"{baseUrl}/Specialist/Consultation/ConfirmBooking?token={token}&confirm=false";

                await _emailService.SendBookingRequestToSpecialistAsync(
                    specialistUser.Email,
                    consultation.SpecialistName,
                    $"{user.FirstName} {user.LastName}",
                    consultation.Title,
                    slotId.HasValue
                        ? consultation.Slots.First(s => s.Id == slotId).DateTime
                        : consultation.DateTime,
                    confirmLink,
                    rejectLink
                );

                TempData["Success"] = "Запит на консультацію надіслано. Очікуйте підтвердження від спеціаліста.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing booking request: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Виникла помилка при обробці запиту.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var consultation = await _consultationRepository.GetByIdAsync(id.Value);
            if (consultation == null)
                return NotFound();

            // Перевіряємо статус бронювання для авторизованого користувача
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                consultation.IsBooked = await _consultationRepository.IsUserBookedForConsultationAsync(consultation.Id, userId);
            }

            return View(consultation);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int consultationId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var consultation = await _context.Consultations
                    .Include(c => c.Slots)
                    .FirstOrDefaultAsync(c => c.Id == consultationId);

                if (consultation == null)
                {
                    TempData["Error"] = "Консультацію не знайдено.";
                    return RedirectToAction("Index", "Profile");
                }

                bool isBooked = false;
                ConsultationSlot bookedSlot = null;

                if (consultation.Format == ConsultationFormat.Individual)
                {
                    bookedSlot = consultation.Slots
                        .FirstOrDefault(s => s.UserId == userId && s.IsBooked);
                    isBooked = bookedSlot != null;
                }
                else
                {
                    isBooked = await _context.ConsultationBookings
                        .AnyAsync(b => b.ConsultationId == consultationId && b.UserId == userId);
                }

                if (!isBooked)
                {
                    TempData["Error"] = "Ви не записані на цю консультацію.";
                    return RedirectToAction("Index", "Profile");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var bookingRequests = await _context.ConsultationBookingRequests
                        .Where(r => r.ConsultationId == consultationId && r.UserId == userId)
                        .ToListAsync();

                    foreach (var request in bookingRequests)
                    {
                        _context.ConsultationBookingRequests.Remove(request);
                    }

                    var bookings = await _context.ConsultationBookings
                        .Where(b => b.ConsultationId == consultationId && b.UserId == userId)
                        .ToListAsync();

                    foreach (var booking in bookings)
                    {
                        _context.ConsultationBookings.Remove(booking);
                    }

                    if (consultation.Format == ConsultationFormat.Individual)
                    {
                        if (bookedSlot != null)
                        {
                            bookedSlot.IsBooked = false;
                            bookedSlot.UserId = null;
                        }

                        var consultationToUpdate = await _context.Consultations
                            .FirstOrDefaultAsync(c => c.Id == consultationId);

                        if (consultationToUpdate != null)
                        {
                            consultationToUpdate.IsBooked = false;
                            consultationToUpdate.UserId = null;
                            _context.Consultations.Update(consultationToUpdate);
                        }
                    }
                    else
                    {
                        consultation.BookedParticipants = Math.Max(0, consultation.BookedParticipants - 1);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Запис на консультацію успішно скасовано.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error canceling consultation booking: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    }
                    TempData["Error"] = "Виникла помилка при скасуванні запису.";
                }

                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Cancel method: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                TempData["Error"] = "Виникла помилка при скасуванні запису.";
                return RedirectToAction("Index", "Profile");
            }
        }

    }
}
