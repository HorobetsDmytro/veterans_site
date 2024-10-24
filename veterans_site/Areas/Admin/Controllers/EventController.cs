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

            // Фільтрація за категорією
            if (category.HasValue)
            {
                events = events.Where(e => e.Category == category);
            }

            // Фільтрація за статусом
            if (status.HasValue)
            {
                events = events.Where(e => e.Status == status);
            }

            // Сортування
            events = sortOrder switch
            {
                "date_desc" => events.OrderByDescending(e => e.Date),
                "participants_asc" => events.OrderBy(e => e.MaxParticipants),
                "participants_desc" => events.OrderByDescending(e => e.MaxParticipants),
                _ => events.OrderBy(e => e.Date),
            };

            // Пагінація
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
        public async Task<IActionResult> Create([Bind("Title,Description,Date,Location,MaxParticipants,Category")] Event @event)
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Date,Location,MaxParticipants,Category,Status")] Event @event)
        {
            if (id != @event.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _eventRepository.UpdateAsync(@event);
                return RedirectToAction(nameof(Index));
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

            var @event = await _eventRepository.GetByIdAsync(id.Value);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

    }
}
