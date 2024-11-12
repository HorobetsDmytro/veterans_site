using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.ViewModels;

namespace veterans_site.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles ="Admin")]
    public class EventController : Controller
    {
        private readonly IEventRepository _eventRepository;
        private const int PageSize = 6;

        public EventController(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<IActionResult> Index(EventCategory? category = null, EventStatus? status = null, string sortOrder = "asc", int page = 1)
        {
            var events = await _eventRepository.GetAllAsync();

            if (category.HasValue)
            {
                events = events.Where(e => e.Category == category);
            }

            if (status.HasValue)
            {
                events = events.Where(e => e.Status == status);
            }

            events = sortOrder switch
            {
                "date_desc" => events.OrderByDescending(e => e.Date),
                "participants_asc" => events.OrderBy(e => e.MaxParticipants),
                "participants_desc" => events.OrderByDescending(e => e.MaxParticipants),
                _ => events.OrderBy(e => e.Date),
            };

            var totalItems = events.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            var pagedEvents = events
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var viewModel = new EventIndexViewModel
            {
                Events = pagedEvents,
                CurrentPage = page,
                TotalPages = totalPages,
                CurrentCategory = category,
                CurrentStatus = status,
                CurrentSort = sortOrder
            };

            return View(viewModel);
        }


        [HttpGet]
        public IActionResult Create()
        {
            var model = new Event
            {
                Status = EventStatus.Planned,
                Date = DateTime.Now
            };
            model.Date = model.Date.AddSeconds(-model.Date.Second).AddMilliseconds(-model.Date.Millisecond);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Date,Duration,Location,MaxParticipants,Category")] Event @event)
        {
            if (ModelState.IsValid)
            {
                @event.Status = EventStatus.Planned;
                await _eventRepository.AddAsync(@event);
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _eventRepository.GetByIdAsync(id.Value);
            if (@event == null)
            {
                return NotFound();
            }
            return View(@event);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event @event)
        {
            if (id != @event.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Отримуємо поточний стан події з бази даних
                    var existingEvent = await _eventRepository.GetByIdAsync(id);
                    if (existingEvent == null)
                    {
                        return NotFound();
                    }

                    // Перевіряємо, чи змінилась дата
                    if (@event.Date > DateTime.Now)
                    {
                        // Якщо нова дата в майбутньому, встановлюємо статус "Заплановано"
                        @event.Status = EventStatus.Planned;
                    }
                    else
                    {
                        // Якщо дата в минулому або теперішньому, зберігаємо поточний статус
                        @event.Status = existingEvent.Status;
                    }

                    // Зберігаємо всі інші поля
                    existingEvent.Title = @event.Title;
                    existingEvent.Description = @event.Description;
                    existingEvent.Date = @event.Date;
                    existingEvent.Location = @event.Location;
                    existingEvent.MaxParticipants = @event.MaxParticipants;
                    existingEvent.Category = @event.Category;
                    existingEvent.Duration = @event.Duration;
                    existingEvent.Status = @event.Status;

                    await _eventRepository.UpdateAsync(existingEvent);

                    TempData["Success"] = "Подію успішно оновлено";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Виникла помилка при збереженні змін. Спробуйте ще раз.");
                }
            }

            return View(@event);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _eventRepository.GetByIdAsync(id.Value);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _eventRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Upcoming()
        {
            var upcomingEvents = await _eventRepository.GetUpcomingEventsAsync();
            return View(upcomingEvents);
        }

        public async Task<IActionResult> SearchByLocation(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                return RedirectToAction(nameof(Index));
            }

            var events = await _eventRepository.GetEventsByLocationAsync(location);
            return View("Index", events);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evt = await _eventRepository.GetByIdWithParticipantsAsync(id.Value);
            if (evt == null)
            {
                return NotFound();
            }

            return View(evt);
        }

    }
}
