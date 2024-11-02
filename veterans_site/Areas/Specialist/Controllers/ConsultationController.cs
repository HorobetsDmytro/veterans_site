using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.Services;
using veterans_site.ViewModels;

namespace veterans_site.Areas.Specialist.Controllers
{
    [Area("Specialist")]
    [Authorize(Roles = "Specialist")]
    public class ConsultationController : Controller
    {
        private readonly IConsultationRepository _consultationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ConsultationController> _logger;
        private readonly VeteranSupportDBContext _context;
        private readonly IEmailService _emailService;
        private const int PageSize = 6;

        public ConsultationController(
            IConsultationRepository consultationRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<ConsultationController> logger,
            VeteranSupportDBContext context,
            IEmailService emailService) 
        {
            _consultationRepository = consultationRepository;
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index(
            ConsultationType? type = null,
            ConsultationFormat? format = null,
            ConsultationStatus? status = null,
            string sortOrder = null,
            int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

            // Отримуємо тільки батьківські консультації або консультації без слотів
            var consultations = await _consultationRepository.GetFilteredConsultationsAsync(
                type, format, status, null, null, sortOrder, page, PageSize, specialistName, true);

            var totalPages = await _consultationRepository.GetTotalPagesAsync(
                type, format, status, null, null, PageSize, specialistName, true);

            var viewModel = new ConsultationIndexViewModel
            {
                Consultations = consultations,
                CurrentType = type,
                CurrentFormat = format,
                CurrentStatus = status,
                CurrentSort = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var currentUser = _userManager.GetUserAsync(User).Result;
            var consultation = new Consultation
            {
                SpecialistName = $"{currentUser.FirstName} {currentUser.LastName}",
                Status = ConsultationStatus.Planned,
                DateTime = DateTime.Now.AddSeconds(-DateTime.Now.Second)
                                  .AddMilliseconds(-DateTime.Now.Millisecond),
                Duration = 10
            };
            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Consultation consultation)
        {
            try
            {
                _logger.LogInformation($"Отримано дані: Format={consultation.Format}, " +
                    $"DateTime={consultation.DateTime}, EndDateTime={consultation.EndDateTime}, " +
                    $"Duration={consultation.Duration}");

                if (consultation.Mode == ConsultationMode.Offline && string.IsNullOrWhiteSpace(consultation.Location))
                {
                    ModelState.AddModelError("Location", "Для офлайн консультації необхідно вказати місце проведення");
                }

                if (!ModelState.IsValid)
                {
                    return View(consultation);
                }

                var currentUser = await _userManager.GetUserAsync(User);
                consultation.SpecialistName = $"{currentUser.FirstName} {currentUser.LastName}";

                if (consultation.Format == ConsultationFormat.Individual)
                {
                    var totalMinutes = (consultation.EndDateTime - consultation.DateTime).Value.TotalMinutes;
                    var slotsCount = (int)(totalMinutes / consultation.Duration);

                    if (slotsCount <= 0)
                    {
                        ModelState.AddModelError("", "Неправильний інтервал часу для створення слотів");
                        return View(consultation);
                    }

                    consultation.IsParent = true;
                    consultation.Status = ConsultationStatus.Planned;
                    consultation.MaxParticipants = slotsCount;
                    await _consultationRepository.AddAsync(consultation);

                    var currentTime = consultation.DateTime;
                    for (int i = 0; i < slotsCount; i++)
                    {
                        var slot = new ConsultationSlot
                        {
                            ConsultationId = consultation.Id,
                            DateTime = currentTime,
                            IsBooked = false
                        };

                        consultation.Slots.Add(slot);
                        currentTime = currentTime.AddMinutes(consultation.Duration);
                    }

                    await _consultationRepository.UpdateAsync(consultation);
                }
                else
                {
                    if (!consultation.MaxParticipants.HasValue || consultation.MaxParticipants < 2)
                    {
                        ModelState.AddModelError("MaxParticipants", "Для групової консультації потрібно вказати кількість учасників (мінімум 2)");
                        return View(consultation);
                    }

                    consultation.IsBooked = false;
                    consultation.BookedParticipants = 0;
                    consultation.Status = ConsultationStatus.Planned;
                    await _consultationRepository.AddAsync(consultation);
                }

                // Отримуємо всіх користувачів з роллю Veteran
                var veterans = await _userManager.GetUsersInRoleAsync("Veteran");

                // Надсилаємо повідомлення кожному ветерану
                foreach (var veteran in veterans)
                {
                    try
                    {
                        await _emailService.SendNewConsultationNotificationAsync(
                            veteran.Email,
                            $"{veteran.FirstName} {veteran.LastName}",
                            consultation
                        );
                    }
                    catch (Exception ex)
                    {
                        // Логуємо помилку, але продовжуємо надсилати іншим ветеранам
                        _logger.LogError($"Failed to send notification to {veteran.Email}: {ex.Message}");
                    }
                }

                TempData["Success"] = "Консультацію успішно створено та надіслано сповіщення всім ветеранам";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка при створенні консультації: {ex.Message}");
                ModelState.AddModelError("", "Виникла помилка при створенні консультації");
                return View(consultation);
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var consultation = await _consultationRepository.GetByIdWithSlotsAsync(id.Value);
            if (consultation == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (consultation.SpecialistName != $"{currentUser.FirstName} {currentUser.LastName}")
                return Forbid();

            return View(consultation);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var consultation = await _consultationRepository.GetByIdWithSlotsAsync(id.Value);
            if (consultation == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (consultation.SpecialistName != $"{currentUser.FirstName} {currentUser.LastName}")
                return Forbid();

            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Consultation consultation)
        {
            if (id != consultation.Id)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (consultation.SpecialistName != $"{currentUser.FirstName} {currentUser.LastName}")
                return Forbid();

            if (consultation.Mode == ConsultationMode.Offline && string.IsNullOrWhiteSpace(consultation.Location))
            {
                ModelState.AddModelError("Location", "Для офлайн консультації необхідно вказати місце проведення");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingConsultation = await _consultationRepository.GetByIdWithSlotsAsync(id);

                    // Оновлюємо основні дані консультації
                    existingConsultation.Title = consultation.Title;
                    existingConsultation.Description = consultation.Description;
                    existingConsultation.Price = consultation.Price;
                    existingConsultation.Type = consultation.Type;
                    existingConsultation.Mode = consultation.Mode;
                    existingConsultation.Location = consultation.Location;
                    existingConsultation.Status = consultation.Status;

                    if (existingConsultation.Format == ConsultationFormat.Individual)
                    {
                        // Оновлюємо слоти, якщо вони не заброньовані
                        foreach (var slot in existingConsultation.Slots.Where(s => !s.IsBooked))
                        {
                            // Оновлення часу слоту відповідно до нової тривалості
                            // Тут потрібна додаткова логіка для перерахунку часу слотів
                        }
                    }
                    else
                    {
                        existingConsultation.MaxParticipants = consultation.MaxParticipants;
                        existingConsultation.DateTime = consultation.DateTime;
                        existingConsultation.Duration = consultation.Duration;
                    }

                    await _consultationRepository.UpdateAsync(existingConsultation);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Помилка при оновленні консультації");
                }
            }
            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteSlot(int slotId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

                // Отримуємо слот разом з консультацією
                var slot = await _context.ConsultationSlots
                    .Include(s => s.Consultation)
                    .FirstOrDefaultAsync(s => s.Id == slotId);

                if (slot == null)
                {
                    return Json(new { success = false, message = "Слот не знайдено" });
                }

                // Перевіряємо чи належить консультація поточному спеціалісту
                if (slot.Consultation.SpecialistName != specialistName)
                {
                    return Json(new { success = false, message = "У вас немає прав для видалення цього слоту" });
                }

                // Перевіряємо чи слот не заброньований
                if (slot.IsBooked)
                {
                    return Json(new { success = false, message = "Неможливо видалити заброньований слот" });
                }

                // Оновлюємо кількість максимальних учасників в основній консультації
                slot.Consultation.MaxParticipants--;

                // Видаляємо слот
                _context.ConsultationSlots.Remove(slot);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка при видаленні слоту: {ex.Message}");
                return Json(new { success = false, message = "Виникла помилка при видаленні слоту" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var consultation = await _consultationRepository.GetByIdWithSlotsAsync(id.Value);
                if (consultation == null)
                {
                    return NotFound();
                }

                var currentUser = await _userManager.GetUserAsync(User);
                var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

                if (consultation.SpecialistName != specialistName)
                {
                    return Forbid();
                }

                return View(consultation);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка при отриманні консультації для видалення: {ex.Message}");
                TempData["Error"] = "Виникла помилка при завантаженні даних консультації";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var consultation = await _context.Consultations
                        .Include(c => c.Slots)
                        .Include(c => c.Bookings)
                        .FirstOrDefaultAsync(c => c.Id == id);

                    if (consultation == null)
                    {
                        TempData["Error"] = "Консультацію не знайдено.";
                        return RedirectToAction(nameof(Index));
                    }

                    var currentUser = await _userManager.GetUserAsync(User);
                    var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

                    if (consultation.SpecialistName != specialistName)
                    {
                        TempData["Error"] = "У вас немає прав для видалення цієї консультації.";
                        return RedirectToAction(nameof(Index));
                    }

                    if (consultation.Format == ConsultationFormat.Individual)
                    {
                        if (consultation.Slots != null && consultation.Slots.Any(s => s.IsBooked))
                        {
                            TempData["Error"] = "Неможливо видалити консультацію, оскільки є заброньовані слоти";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    else if (consultation.BookedParticipants > 0)
                    {
                        TempData["Error"] = "Неможливо видалити консультацію, оскільки є зареєстровані учасники";
                        return RedirectToAction(nameof(Index));
                    }

                    var bookingRequests = await _context.ConsultationBookingRequests
                        .Where(r => r.ConsultationId == id)
                        .ToListAsync();

                    foreach (var request in bookingRequests)
                    {
                        _context.ConsultationBookingRequests.Remove(request);
                    }

                    var bookings = await _context.ConsultationBookings
                        .Where(b => b.ConsultationId == id)
                        .ToListAsync();

                    foreach (var booking in bookings)
                    {
                        _context.ConsultationBookings.Remove(booking);
                    }

                    if (consultation.Slots != null)
                    {
                        foreach (var slot in consultation.Slots)
                        {
                            _context.ConsultationSlots.Remove(slot);
                        }
                    }

                    _context.Consultations.Remove(consultation);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Консультацію успішно видалено";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting consultation: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                TempData["Error"] = "Виникла помилка при видаленні консультації";
                return RedirectToAction(nameof(Index));
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ConfirmBooking(string token, bool confirm)
        {
            try
            {
                var request = await _context.ConsultationBookingRequests
                    .Include(r => r.User)
                    .Include(r => r.Consultation)
                    .Include(r => r.Slot)
                    .FirstOrDefaultAsync(r => r.Token == token && !r.IsApproved.HasValue && r.ExpiryTime > DateTime.UtcNow);

                if (request == null)
                {
                    return View("Error", new ErrorViewModel { Message = "Запит не знайдено або він вже не активний." });
                }

                if (confirm)
                {
                    bool bookingSuccess;
                    if (request.SlotId.HasValue)
                    {
                        bookingSuccess = await _consultationRepository.BookConsultationSlotAsync(
                            request.ConsultationId, request.SlotId.Value, request.UserId);
                    }
                    else
                    {
                        bookingSuccess = await _consultationRepository.BookConsultationAsync(
                            request.ConsultationId, request.UserId);
                    }

                    if (bookingSuccess)
                    {
                        request.IsApproved = true;
                        await _context.SaveChangesAsync();

                        try
                        {
                            await _emailService.SendConsultationConfirmationAsync(
                                request.User.Email,
                                $"{request.User.FirstName} {request.User.LastName}",
                                request.Consultation.Title,
                                request.SlotId.HasValue
                                    ? request.Slot.DateTime
                                    : request.Consultation.DateTime
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error sending confirmation email: {ex.Message}");
                        }

                        return View("BookingConfirmed");
                    }
                    else
                    {
                        return View("Error", new ErrorViewModel { Message = "Не вдалося підтвердити бронювання." });
                    }
                }
                else
                {
                    request.IsApproved = false;
                    await _context.SaveChangesAsync();

                    try
                    {
                        await _emailService.SendBookingRejectedEmailAsync(
                            request.User.Email,
                            $"{request.User.FirstName} {request.User.LastName}",
                            request.Consultation.Title
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error sending rejection email: {ex.Message}");
                    }

                    return View("BookingRejected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ConfirmBooking: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return View("Error", new ErrorViewModel { Message = "Виникла помилка при обробці запиту." });
            }
        }
    }
}