using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.Services;
using veterans_site.ViewModels;

namespace veterans_site.Controllers
{
    public class EventsController : Controller
    {
        private readonly IEventRepository _eventRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<EventsController> _logger;
        private readonly VeteranSupportDBContext _context;
        private const int PageSize = 6;

        public EventsController(
            IEventRepository eventRepository,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<EventsController> logger,
            VeteranSupportDBContext context)
        {
            _eventRepository = eventRepository;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(
            EventCategory? category = null,
            string sortOrder = null,
            int page = 1)
        {
            try
            {
                IEnumerable<Event> events = await _eventRepository.GetAllAsync();

                // Фільтруємо тільки заплановані та активні події
                events = events.Where(e => e.Status == EventStatus.Planned || e.Status == EventStatus.InProgress);

                // Застосовуємо фільтр категорії
                if (category.HasValue)
                {
                    events = events.Where(e => e.Category == category);
                }

                // Застосовуємо сортування
                events = sortOrder switch
                {
                    "date_desc" => events.OrderByDescending(e => e.Date),
                    "participants_asc" => events.OrderBy(e => e.EventParticipants.Count),
                    "participants_desc" => events.OrderByDescending(e => e.EventParticipants.Count),
                    _ => events.OrderBy(e => e.Date)
                };

                // Підраховуємо загальну кількість сторінок
                var totalItems = events.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

                // Застосовуємо пагінацію
                var pagedEvents = events
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Якщо користувач авторизований, перевіряємо його реєстрації
                if (User.Identity.IsAuthenticated)
                {
                    var userId = _userManager.GetUserId(User);
                    foreach (var evt in pagedEvents)
                    {
                        evt.EventParticipants = await _eventRepository.GetEventParticipantsAsync(evt.Id);
                    }
                }

                var viewModel = new EventIndexViewModel
                {
                    Events = pagedEvents,
                    CurrentCategory = category,
                    CurrentSort = sortOrder,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Index method: {ex.Message}");
                return View("Error");
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var evt = await _eventRepository.GetByIdWithParticipantsAsync(id.Value);
                if (evt == null)
                {
                    return NotFound();
                }

                // Якщо користувач авторизований, перевіряємо чи він зареєстрований
                if (User.Identity.IsAuthenticated)
                {
                    var userId = _userManager.GetUserId(User);
                    ViewBag.IsRegistered = await _eventRepository.IsUserRegisteredForEventAsync(id.Value, userId);
                }

                return View(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Details method: {ex.Message}");
                return View("Error");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Book(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (!user.IsActive)
                {
                    TempData["Error"] = "Ваш обліковий запис неактивний. Ви не можете реєструватися на події.";
                    return RedirectToAction(nameof(Index));
                }

                var evt = await _eventRepository.GetByIdWithParticipantsAsync(id.Value);
                if (evt == null)
                {
                    return NotFound();
                }

                if (evt.Status != EventStatus.Planned)
                {
                    TempData["Error"] = "Реєстрація на цю подію вже закрита.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var userId = _userManager.GetUserId(User);
                if (await _eventRepository.IsUserRegisteredForEventAsync(id.Value, userId))
                {
                    TempData["Error"] = "Ви вже зареєстровані на цю подію.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (!evt.CanRegister)
                {
                    TempData["Error"] = "На жаль, всі місця вже зайняті.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Book GET method: {ex.Message}");
                return View("Error");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                if (await _eventRepository.IsUserRegisteredForEventAsync(id, userId))
                {
                    TempData["Error"] = "Ви вже зареєстровані на цю подію.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var evt = await _eventRepository.GetByIdWithParticipantsAsync(id);
                if (evt == null)
                {
                    return NotFound();
                }

                if (evt.Status != EventStatus.Planned)
                {
                    TempData["Error"] = "Реєстрація на цю подію вже закрита.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (!evt.CanRegister)
                {
                    TempData["Error"] = "На жаль, всі місця вже зайняті.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _eventRepository.RegisterUserForEventAsync(userId, id);

                // Тут можна додати відправку email з підтвердженням реєстрації
                // await _emailService.SendEventRegistrationConfirmationAsync(user.Email, user.FirstName, evt);

                TempData["Success"] = "Ви успішно зареєструвалися на подію.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Book POST method: {ex.Message}");
                TempData["Error"] = "Виникла помилка при реєстрації на подію.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                if (!await _eventRepository.IsUserRegisteredForEventAsync(id, userId))
                {
                    TempData["Error"] = "Ви не зареєстровані на цю подію.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var evt = await _eventRepository.GetByIdAsync(id);
                if (evt == null)
                {
                    return NotFound();
                }

                if (evt.Date <= DateTime.Now)
                {
                    TempData["Error"] = "Не можна скасувати реєстрацію на подію, що вже почалась або завершилась.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _eventRepository.CancelEventParticipationAsync(id, userId);

                TempData["Success"] = "Реєстрацію на подію скасовано.";
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Cancel method: {ex.Message}");
                TempData["Error"] = "Виникла помилка при скасуванні реєстрації.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}