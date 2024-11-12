using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INewsRepository _newsRepository;
        private readonly IEventRepository _eventRepository;
        private readonly VeteranSupportDBContext _context;

        public HomeController(INewsRepository newsRepository, IEventRepository eventRepository, VeteranSupportDBContext context, UserManager<ApplicationUser> userManager)
        {
            _newsRepository = newsRepository;
            _eventRepository = eventRepository;
            _context = context;
            _userManager = userManager;
        }

        private async Task<HomeStatisticsViewModel> GetStatisticsAsync()
        {
            // Кількість ветеранів, які записувались на консультації
            var veteransCount = await _context.ConsultationBookings
                .Select(b => b.UserId)
                .Distinct()
                .CountAsync();

            // Кількість завершених консультацій
            var completedConsultations = await _context.Consultations
                .Where(c => c.Status == ConsultationStatus.Completed)
                .CountAsync();

            // Кількість завершених подій
            var completedEvents = await _context.Events
                .Where(e => e.Status == EventStatus.Completed)
                .CountAsync();

            // Кількість спеціалістів
            var specialistsCount = (await _userManager.GetUsersInRoleAsync("Specialist")).Count;

            return new HomeStatisticsViewModel
            {
                TotalVeterans = veteransCount,
                CompletedConsultations = completedConsultations,
                CompletedEvents = completedEvents,
                TotalSpecialists = specialistsCount
            };
        }


        public async Task<IActionResult> Index()
        {
            var statistics = await GetStatisticsAsync();
            var latestNews = await _newsRepository.GetLatestNewsAsync(3);
            var upcomingEvents = await _eventRepository.GetUpcomingEventsAsync();

            ViewBag.Statistics = statistics;
            ViewBag.LatestNews = latestNews;
            ViewBag.UpcomingEvents = upcomingEvents
                .OrderBy(e => e.Date)
                .ToList();

            return View();
        }
    }
}
