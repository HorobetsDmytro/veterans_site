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
        private readonly VeteranSupportDbContext _context;
        private readonly IEmailService _emailService;
        private const int PageSize = 6;

        public ConsultationController(
            IConsultationRepository consultationRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<ConsultationController> logger,
            VeteranSupportDbContext context,
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
                await _consultationRepository.UpdateConsultationStatusesAsync();

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
                        ModelState.AddModelError("MaxParticipants",
                            "Для групової консультації потрібно вказати кількість учасників (мінімум 2)");
                        return View(consultation);
                    }

                    consultation.IsBooked = false;
                    consultation.BookedParticipants = 0;
                    consultation.Status = ConsultationStatus.Planned;
                    await _consultationRepository.AddAsync(consultation);
                }

                var veterans = await _userManager.GetUsersInRoleAsync("Veteran");

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
            {
                return NotFound();
            }

            var consultation = await _context.Consultations
                .Include(c => c.Slots)
                .Include(c => c.Bookings)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consultation == null)
            {
                return NotFound();
            }

            bool hasBookings = consultation.Format == ConsultationFormat.Individual
                ? consultation.Slots.Any(s => s.IsBooked)
                : consultation.Bookings.Any();

            if (hasBookings)
            {
                TempData["Error"] = "Неможливо редагувати консультацію, оскільки на неї вже є записи";
                return RedirectToAction(nameof(Index));
            }

            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Consultation consultation)
        {
            if (id != consultation.Id)
            {
                return NotFound();
            }

            var existingConsultation = await _context.Consultations
                .Include(c => c.Slots)
                .Include(c => c.Bookings)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existingConsultation == null)
            {
                return NotFound();
            }

            bool hasBookings = existingConsultation.Format == ConsultationFormat.Individual
                ? existingConsultation.Slots.Any(s => s.IsBooked)
                : existingConsultation.Bookings.Any();

            if (hasBookings)
            {
                TempData["Error"] = "Неможливо редагувати консультацію, оскільки на неї вже є записи";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingConsultation.Title = consultation.Title;
                    existingConsultation.Description = consultation.Description;
                    existingConsultation.Type = consultation.Type;
                    existingConsultation.Mode = consultation.Mode;
                    existingConsultation.Duration = consultation.Duration;
                    existingConsultation.Price = consultation.Price;
                    existingConsultation.Location = consultation.Location;

                    if (consultation.Format == ConsultationFormat.Individual)
                    {
                        existingConsultation.DateTime = consultation.DateTime;
                        existingConsultation.EndDateTime = consultation.EndDateTime;

                        _context.ConsultationSlots.RemoveRange(existingConsultation.Slots);

                        var currentTime = consultation.DateTime;
                        while (currentTime < consultation.EndDateTime)
                        {
                            var newSlot = new ConsultationSlot
                            {
                                ConsultationId = id,
                                DateTime = currentTime,
                                IsBooked = false
                            };
                            existingConsultation.Slots.Add(newSlot);
                            currentTime = currentTime.AddMinutes(consultation.Duration);
                        }
                    }
                    else
                    {
                        existingConsultation.DateTime = consultation.DateTime;
                        existingConsultation.MaxParticipants = consultation.MaxParticipants;
                    }

                    var now = DateTime.Now;
                    if (consultation.DateTime > now)
                    {
                        existingConsultation.Status = ConsultationStatus.Planned;
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Консультацію успішно оновлено";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating consultation: {ex.Message}");
                    ModelState.AddModelError("", "Виникла помилка при збереженні змін.");
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
                    if (consultation.Status != ConsultationStatus.Completed)
                    {
                        if (consultation.Format == ConsultationFormat.Individual)
                        {
                            if (consultation.Slots.Any(s => s.IsBooked))
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
                    }

                    if (consultation.Slots != null)
                    {
                        foreach (var slot in consultation.Slots.ToList())
                        {
                            _context.ConsultationSlots.Remove(slot);
                        }
                    }

                    var bookings = await _context.ConsultationBookings
                        .Where(b => b.ConsultationId == id)
                        .ToListAsync();

                    foreach (var booking in bookings)
                    {
                        _context.ConsultationBookings.Remove(booking);
                    }

                    var bookingRequests = await _context.ConsultationBookingRequests
                        .Where(r => r.ConsultationId == id)
                        .ToListAsync();

                    foreach (var request in bookingRequests)
                    {
                        _context.ConsultationBookingRequests.Remove(request);
                    }

                    _context.Consultations.Remove(consultation);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Консультацію успішно видалено";
                    _logger.LogInformation(
                        $"Consultation {id} was successfully deleted by specialist {specialistName}");
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
                    .FirstOrDefaultAsync(r =>
                        r.Token == token && !r.IsApproved.HasValue && r.ExpiryTime > DateTime.UtcNow);

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
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var booking = await _context.ConsultationBookings
                            .FirstOrDefaultAsync(b => b.ConsultationId == request.ConsultationId &&
                                                      b.UserId == request.UserId);
                        if (booking != null)
                        {
                            _context.ConsultationBookings.Remove(booking);
                        }

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

                        if (request.Consultation.Format == ConsultationFormat.Group)
                        {
                            request.Consultation.BookedParticipants =
                                Math.Max(0, request.Consultation.BookedParticipants - 1);
                        }

                        _context.ConsultationBookingRequests.Remove(request);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

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

                booking.Consultation.BookedParticipants--;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var consultation = await _consultationRepository.GetByIdAsync(id);
                if (consultation == null)
                {
                    return NotFound();
                }

                if (consultation.Status != ConsultationStatus.Planned)
                {
                    TempData["Error"] = "Можна скасовувати тільки заплановані консультації";
                    return RedirectToAction(nameof(Index));
                }

                consultation.Status = ConsultationStatus.Cancelled;
                await _consultationRepository.UpdateAsync(consultation);

                TempData["Success"] = "Консультацію успішно скасовано";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling consultation: {ex.Message}");
                TempData["Error"] = "Виникла помилка при скасуванні консультації";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Uncancel(int id)
        {
            try
            {
                var consultation = await _consultationRepository.GetByIdAsync(id);
                if (consultation == null)
                {
                    return NotFound();
                }

                if (consultation.Status != ConsultationStatus.Cancelled)
                {
                    TempData["Error"] = "Можна активувати тільки скасовані консультації";
                    return RedirectToAction(nameof(Index));
                }

                if (consultation.DateTime < DateTime.Now)
                {
                    TempData["Error"] = "Не можна активувати консультацію з минулою датою";
                    return RedirectToAction(nameof(Index));
                }

                consultation.Status = ConsultationStatus.Planned;
                await _consultationRepository.UpdateAsync(consultation);

                TempData["Success"] = "Консультацію успішно активовано знову";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uncancelling consultation: {ex.Message}");
                TempData["Error"] = "Виникла помилка при активації консультації";
                return RedirectToAction(nameof(Index));
            }
        }

        public class CancelBookingViewModel
        {
            public int Id { get; set; }
        }
        
        [HttpGet]
        public async Task<IActionResult> Statistics(string period = "all")
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";
                
                var statistics = new SpecialistStatisticsViewModel();
                
                DateTime? startDate = null;
                string periodLabel = "Весь час";
                
                switch (period)
                {
                    case "month":
                        startDate = DateTime.Now.AddMonths(-1);
                        periodLabel = "Останній місяць";
                        break;
                    case "quarter":
                        startDate = DateTime.Now.AddMonths(-3);
                        periodLabel = "Останній квартал";
                        break;
                    case "halfyear":
                        startDate = DateTime.Now.AddMonths(-6);
                        periodLabel = "Останні 6 місяців";
                        break;
                    case "year":
                        startDate = DateTime.Now.AddYears(-1);
                        periodLabel = "Останній рік";
                        break;
                }
                
                var consultationsQuery = _context.Consultations
                    .Where(c => c.SpecialistName == specialistName);
                    
                if (startDate.HasValue)
                {
                    consultationsQuery = consultationsQuery.Where(c => c.DateTime >= startDate.Value);
                }
                
                var consultations = await consultationsQuery.ToListAsync();
                
                statistics.TotalConsultations = consultations.Count;
                statistics.UpcomingConsultations = consultations
                    .Count(c => c.Status == ConsultationStatus.Planned && c.DateTime > DateTime.Now);
                statistics.CompletedConsultations = consultations
                    .Count(c => c.Status == ConsultationStatus.Completed);
                statistics.CancelledConsultations = consultations
                    .Count(c => c.Status == ConsultationStatus.Cancelled);
                statistics.PeriodLabel = periodLabel;
                
                if (startDate.HasValue)
                {
                    var previousStartDate = startDate.Value.AddDays(-(DateTime.Now - startDate.Value).TotalDays);
                    var previousConsultations = await _context.Consultations
                        .Where(c => c.SpecialistName == specialistName &&
                                c.DateTime >= previousStartDate &&
                                c.DateTime < startDate.Value)
                        .ToListAsync();
                        
                    statistics.PreviousPeriodConsultations = previousConsultations.Count;
                    statistics.PreviousPeriodCompleted = previousConsultations.Count(c => c.Status == ConsultationStatus.Completed);
                }
                
                statistics.ConsultationsByType = consultations
                    .GroupBy(c => c.Type)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                statistics.ConsultationsByFormat = consultations
                    .GroupBy(c => c.Format)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                var monthlyTrend = new Dictionary<string, MonthlyStats>();
                var startDate1 = DateTime.Now.AddMonths(-12);
                var endDate = DateTime.Now;

                for (var date = startDate1; date <= endDate; date = date.AddMonths(1))
                {
                    var key = $"{date.Year}-{date.Month:D2}";
                    monthlyTrend[key] = new MonthlyStats { Total = 0, Completed = 0, Cancelled = 0 };
                }   

                var twelveMonthsAgo = DateTime.Now.AddMonths(-12);
                var monthlyData = await _context.Consultations
                    .Where(c => c.SpecialistName == specialistName && c.DateTime >= twelveMonthsAgo)
                    .ToListAsync();

                var dataByMonth = monthlyData
                    .GroupBy(c => new { Year = c.DateTime.Year, Month = c.DateTime.Month })
                    .ToDictionary(
                        g => $"{g.Key.Year}-{g.Key.Month:D2}",
                        g => new MonthlyStats {
                            Total = g.Count(),
                            Completed = g.Count(c => c.Status == ConsultationStatus.Completed),
                            Cancelled = g.Count(c => c.Status == ConsultationStatus.Cancelled)
                        }
                    );

                foreach (var item in dataByMonth)
                {
                    if (monthlyTrend.ContainsKey(item.Key))
                    {
                        monthlyTrend[item.Key] = item.Value;
                    }
                }

                statistics.MonthlyTrend = monthlyTrend;
                
                var groupConsultations = consultations
                    .Where(c => c.Format == ConsultationFormat.Group && c.MaxParticipants.HasValue && c.MaxParticipants > 0);
                
                if (groupConsultations.Any())
                {
                    statistics.AverageGroupAttendance = groupConsultations
                        .Average(c => (double)c.BookedParticipants / c.MaxParticipants.Value * 100);
                        
                    statistics.AttendanceByType = groupConsultations
                        .GroupBy(c => c.Type)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Average(c => (double)c.BookedParticipants / c.MaxParticipants.Value * 100)
                        );
                }

                if (consultations.Any(c => c.Price > 0))
                {
                    var monthlyEarningsData = await _context.Consultations
                        .Where(c => c.SpecialistName == specialistName && c.DateTime >= twelveMonthsAgo)
                        .Include(c => c.Slots)
                        .ToListAsync();

                    var monthlyEarningsWithClients = monthlyEarningsData
                        .GroupBy(c => new { c.DateTime.Year, c.DateTime.Month })
                        .OrderBy(g => g.Key.Year)
                        .ThenBy(g => g.Key.Month)
                        .Select(g => new {
                            Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                            Earnings = g.Sum(c => c.Format == ConsultationFormat.Individual ? 
                                c.Slots.Count(s => s.IsBooked) * c.Price : 
                                c.BookedParticipants * c.Price),
                            Clients = g.Sum(c => c.Format == ConsultationFormat.Individual ? 
                                c.Slots.Count(s => s.IsBooked) : 
                                c.BookedParticipants)
                        })
                        .ToList();

                    statistics.MonthlyEarnings = monthlyEarningsWithClients
                        .ToDictionary(m => m.Month, m => m.Earnings);
    
                    statistics.TotalEarnings = monthlyEarningsWithClients.Sum(m => m.Earnings);

                    Console.WriteLine(statistics.TotalEarnings);
        
                    var totalIndividualBookedSlots = consultations
                        .Where(c => c.Format == ConsultationFormat.Individual && c.Status == ConsultationStatus.Completed && c.Price > 0)
                        .Sum(c => c.Slots.Count(s => s.IsBooked));
                    
                    Console.WriteLine(totalIndividualBookedSlots);

                    var totalGroupParticipants = consultations
                        .Where(c => c.Format == ConsultationFormat.Group && c.Status == ConsultationStatus.Completed && c.Price > 0)
                        .Sum(c => c.BookedParticipants);

                    Console.WriteLine(totalGroupParticipants);

                    var totalClients = totalIndividualBookedSlots + totalGroupParticipants;

                    statistics.AverageEarningsPerConsultation = totalClients > 0 ? 
                        statistics.TotalEarnings / totalClients : 0;
                }
                
                var ninetyDaysAgo = DateTime.Now.AddDays(-90);
                var dailyConsultationsData = await _context.Consultations
                    .Where(c => c.SpecialistName == specialistName && c.DateTime >= ninetyDaysAgo)
                    .ToListAsync();
                    
                statistics.DailyConsultationCounts = dailyConsultationsData
                    .GroupBy(c => c.DateTime.Date.ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.Count());
                
                var veteranBookings = await _context.ConsultationBookings
                    .Include(b => b.User)
                    .Include(b => b.Consultation)
                    .Where(b => b.Consultation.SpecialistName == specialistName)
                    .ToListAsync();
                    
                if (veteranBookings.Any())
                {
                    statistics.UniqueVeteransServed = veteranBookings
                        .Select(b => b.UserId)
                        .Distinct()
                        .Count();
                        
                    var veteranVisitCounts = veteranBookings
                        .GroupBy(b => b.UserId)
                        .Select(g => g.Count())
                        .ToList();
                        
                    if (veteranVisitCounts.Any())
                    {
                        statistics.RepeatVisitRate = (double)(veteranVisitCounts.Count(c => c > 1)) / veteranVisitCounts.Count * 100;
                    }
                }
                
                var individualSlots = await _context.ConsultationSlots
                    .Include(s => s.Consultation)
                    .Where(s => s.Consultation.SpecialistName == specialistName && s.IsBooked)
                    .ToListAsync();
                    
                if (individualSlots.Any())
                {
                    statistics.PopularTimeSlots = individualSlots
                        .GroupBy(s => s.DateTime.Hour)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .ToDictionary(g => g.Key, g => g.Count());
                }
                
                return View(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading statistics: {ex.Message}");
                TempData["Error"] = "Виникла помилка при завантаженні статистики";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}