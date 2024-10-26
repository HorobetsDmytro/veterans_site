using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Controllers
{
    public class HomeController : Controller
    {
        private readonly INewsRepository _newsRepository;
        private readonly IEventRepository _eventRepository;

        public HomeController(INewsRepository newsRepository, IEventRepository eventRepository)
        {
            _newsRepository = newsRepository;
            _eventRepository = eventRepository;
        }

        public async Task<IActionResult> Index()
        {
            var latestNews = await _newsRepository.GetLatestNewsAsync(3);
            var upcomingEvents = await _eventRepository.GetUpcomingEventsAsync();

            ViewBag.LatestNews = latestNews;
            ViewBag.UpcomingEvents = upcomingEvents;

            return View();
        }
    }
}
