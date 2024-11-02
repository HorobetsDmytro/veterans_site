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
    double? minPrice = null,
    double? maxPrice = null,
    string sortOrder = null,
    int page = 1)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

                var consultations = await _consultationRepository.GetFilteredConsultationsAsync(
                    type: type,
                    format: format,
                    status: status,
                    minPrice: minPrice,
                    maxPrice: maxPrice,
                    sortOrder: sortOrder,
                    page: page,
                    pageSize: PageSize,
                    specialistName: specialistName,
                    parentOnly: false);

                var totalPages = await _consultationRepository.GetTotalPagesAsync(
                    type: type,
                    format: format,
                    status: status,
                    minPrice: minPrice,
                    maxPrice: maxPrice,
                    pageSize: PageSize,
                    specialistName: specialistName,
                    parentOnly: false);

                var viewModel = new ConsultationIndexViewModel
                {
                    Consultations = consultations,
                    CurrentType = type,
                    CurrentFormat = format,
                    CurrentStatus = status,
                    CurrentSort = sortOrder,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Index method: {ex.Message}");
                TempData["Error"] = "Виникла помилка при завантаженні консультацій";
                return View(new ConsultationIndexViewModel());
            }
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

                    // Перевіряємо чи змінився статус на "Скасовано"
                    var statusChangedToCancelled = existingConsultation.Status != ConsultationStatus.Cancelled &&
                                                 consultation.Status == ConsultationStatus.Cancelled;

                    // Зберігаємо користувачів для повідомлення перед оновленням
                    var usersToNotify = new List<ApplicationUser>();
                    if (statusChangedToCancelled)
                    {
                        if (existingConsultation.Format == ConsultationFormat.Individual)
                        {
                            // Збираємо користувачів із заброньованих слотів
                            usersToNotify.AddRange(
                                existingConsultation.Slots
                                    .Where(s => s.IsBooked && s.User != null)
                                    .Select(s => s.User)
                            );
                        }
                        else
                        {
                            // Збираємо користувачів із бронювань для групової консультації
                            usersToNotify.AddRange(
                                existingConsultation.Bookings
                                    .Where(b => b.User != null)
                                    .Select(b => b.User)
                            );
                        }
                    }

                    // Оновлюємо дані консультації
                    existingConsultation.Title = consultation.Title;
                    existingConsultation.Description = consultation.Description;
                    existingConsultation.Price = consultation.Price;
                    existingConsultation.Type = consultation.Type;
                    existingConsultation.Mode = consultation.Mode;
                    existingConsultation.Location = consultation.Location;
                    existingConsultation.Status = consultation.Status;

                    if (existingConsultation.Format == ConsultationFormat.Group)
                    {
                        existingConsultation.MaxParticipants = consultation.MaxParticipants;
                        existingConsultation.DateTime = consultation.DateTime;
                        existingConsultation.Duration = consultation.Duration;
                    }

                    await _consultationRepository.UpdateAsync(existingConsultation);

                    // Відправляємо повідомлення про скасування
                    if (statusChangedToCancelled)
                    {
                        foreach (var user in usersToNotify)
                        {
                            try
                            {
                                await _emailService.SendConsultationCancelledNotificationAsync(
                                    user.Email,
                                    $"{user.FirstName} {user.LastName}",
                                    existingConsultation.Title,
                                    existingConsultation.Format == ConsultationFormat.Individual
                                        ? existingConsultation.Slots.First(s => s.UserId == user.Id).DateTime
                                        : existingConsultation.DateTime,
                                    existingConsultation.SpecialistName
                                );
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Failed to send cancellation notification to {user.Email}: {ex.Message}");
                            }
                        }
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating consultation: {ex.Message}");
                    ModelState.AddModelError("", "Помилка при оновленні консультації");
                }
            }
            return View(consultation);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSlot([FromBody] DeleteSlotViewModel model)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

                var slot = await _context.ConsultationSlots
                    .Include(s => s.Consultation)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (slot == null)
                {
                    return Json(new { success = false, message = "Слот не знайдено" });
                }

                if (slot.Consultation.SpecialistName != specialistName)
                {
                    return Json(new { success = false, message = "У вас немає прав для видалення цього слоту" });
                }

                if (slot.IsBooked)
                {
                    return Json(new { success = false, message = "Неможливо видалити заброньований слот" });
                }

                _context.ConsultationSlots.Remove(slot);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting slot: {ex.Message}");
                return Json(new { success = false, message = "Виникла помилка при видаленні слоту" });
            }
        }

        public class DeleteSlotViewModel
        {
            public int Id { get; set; }
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
                var consultation = await _consultationRepository.GetByIdWithSlotsAsync(id);
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

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Перевіряємо статус консультації
                    if (consultation.Status != ConsultationStatus.Completed)
                    {
                        // Перевірка на заброньовані слоти для індивідуальних консультацій
                        if (consultation.Format == ConsultationFormat.Individual)
                        {
                            if (consultation.Slots.Any(s => s.IsBooked))
                            {
                                TempData["Error"] = "Неможливо видалити консультацію, оскільки є заброньовані слоти";
                                return RedirectToAction(nameof(Index));
                            }
                        }
                        // Перевірка для групових консультацій
                        else if (consultation.BookedParticipants > 0)
                        {
                            TempData["Error"] = "Неможливо видалити консультацію, оскільки є зареєстровані учасники";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    // Видаляємо всі слоти для індивідуальних консультацій
                    if (consultation.Slots != null)
                    {
                        foreach (var slot in consultation.Slots.ToList())
                        {
                            _context.ConsultationSlots.Remove(slot);
                        }
                    }

                    // Видаляємо всі бронювання
                    var bookings = await _context.ConsultationBookings
                        .Where(b => b.ConsultationId == id)
                        .ToListAsync();

                    foreach (var booking in bookings)
                    {
                        _context.ConsultationBookings.Remove(booking);
                    }

                    // Видаляємо всі запити на бронювання
                    var bookingRequests = await _context.ConsultationBookingRequests
                        .Where(r => r.ConsultationId == id)
                        .ToListAsync();

                    foreach (var request in bookingRequests)
                    {
                        _context.ConsultationBookingRequests.Remove(request);
                    }

                    // Видаляємо саму консультацію
                    _context.Consultations.Remove(consultation);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Консультацію успішно видалено";
                    _logger.LogInformation($"Consultation {id} was successfully deleted by specialist {specialistName}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                return RedirectToAction(nameof(Index));
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

                        // Відправляємо підтвердження користувачу
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
                else // Відхилення запиту
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Видаляємо запис про бронювання, якщо він вже є
                        var booking = await _context.ConsultationBookings
                            .FirstOrDefaultAsync(b => b.ConsultationId == request.ConsultationId &&
                                                    b.UserId == request.UserId);
                        if (booking != null)
                        {
                            _context.ConsultationBookings.Remove(booking);
                        }

                        // Якщо це індивідуальна консультація, очищаємо дані слоту
                        if (request.SlotId.HasValue)
                        {
                            var slot = await _context.ConsultationSlots
                                .FirstOrDefaultAsync(s => s.Id == request.SlotId);
                            if (slot != null)
                            {
                                slot.IsBooked = false;
                                slot.UserId = null;
                            }
                        }

                        // Зменшуємо лічильник учасників для групової консультації
                        if (request.Consultation.Format == ConsultationFormat.Group)
                        {
                            request.Consultation.BookedParticipants =
                                Math.Max(0, request.Consultation.BookedParticipants - 1);
                        }

                        // Видаляємо сам запит
                        _context.ConsultationBookingRequests.Remove(request);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Відправляємо повідомлення про відхилення
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
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError($"Error rejecting booking: {ex.Message}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ConfirmBooking: {ex.Message}");
                return View("Error", new ErrorViewModel { Message = "Виникла помилка при обробці запиту." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking([FromBody] CancelBookingViewModel model)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var booking = await _context.ConsultationBookings
                    .Include(b => b.Consultation)
                    .FirstOrDefaultAsync(b => b.Id == model.Id);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Бронювання не знайдено" });
                }

                if (booking.Consultation.SpecialistName != $"{currentUser.FirstName} {currentUser.LastName}")
                {
                    return Json(new { success = false, message = "У вас немає прав для скасування цього бронювання" });
                }

                // Оновлюємо кількість учасників
                booking.Consultation.BookedParticipants--;

                // Видаляємо бронювання
                _context.ConsultationBookings.Remove(booking);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error canceling booking: {ex.Message}");
                return Json(new { success = false, message = "Виникла помилка при скасуванні реєстрації" });
            }
        }

        public class CancelBookingViewModel
        {
            public int Id { get; set; }
        }
    }
}