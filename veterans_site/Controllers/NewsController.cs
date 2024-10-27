using Microsoft.AspNetCore.Mvc;
using System.Drawing.Printing;
using veterans_site.Interfaces;
using veterans_site.ViewModels;

namespace veterans_site.Controllers
{
    public class NewsController : Controller
    {
        private readonly INewsRepository _newsRepository;
        private const int PageSize = 6;

        public NewsController(INewsRepository newsRepository)
        {
            _newsRepository = newsRepository;
        }

        // GET: News
        public async Task<IActionResult> Index(string searchTitle, string sortOrder = "latest", int page = 1)
        {
            // Зберігаємо параметри для пагінації
            ViewBag.CurrentSearch = searchTitle;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentPage = page;

            // Отримуємо всі новини
            var query = await _newsRepository.GetAllAsync();

            // Застосовуємо фільтр пошуку
            if (!string.IsNullOrEmpty(searchTitle))
            {
                query = query.Where(n => n.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase));
            }

            // Застосовуємо сортування
            query = sortOrder switch
            {
                "oldest" => query.OrderBy(n => n.PublishDate),
                _ => query.OrderByDescending(n => n.PublishDate)
            };

            // Підраховуємо загальну кількість сторінок
            var totalItems = query.Count();
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            // Застосовуємо пагінацію
            var news = query
            .Skip((page - 1) * PageSize)
                .Take(PageSize);

            return View(news);
        }

        // GET: News/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _newsRepository.GetByIdAsync(id.Value);
            if (news == null)
            {
                return NotFound();
            }

            // Отримуємо список останніх новин для відображення збоку
            var recentNews = await _newsRepository.GetLatestNewsAsync(5);
            var recentNewsExceptCurrent = recentNews.Where(n => n.Id != id);

            var viewModel = new NewsDetailsViewModel
            {
                News = news,
                RecentNews = recentNewsExceptCurrent
            };

            return View(viewModel);
        }
    }
}
