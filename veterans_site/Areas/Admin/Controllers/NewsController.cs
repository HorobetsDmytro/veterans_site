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
    [Authorize(Roles = "Admin")]
    public class NewsController : Controller
    {
        private readonly VeteranSupportDBContext _context;
        private readonly INewsRepository _newsRepository;
        private readonly IWebHostEnvironment _hostEnvironment;
        private const int PageSize = 6;

        public NewsController(VeteranSupportDBContext context, INewsRepository newsRepository, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _newsRepository = newsRepository;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index(string searchTitle = null, string sortOrder = "asc", int page = 1)
        {
            var newsItems = await _newsRepository.GetAllAsync();

            if (!string.IsNullOrEmpty(searchTitle))
            {
                newsItems = newsItems.Where(n => n.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase));
            }

            newsItems = sortOrder switch
            {
                "date_desc" => newsItems.OrderByDescending(n => n.PublishDate),
                _ => newsItems.OrderBy(n => n.PublishDate),
            };

            var totalItems = newsItems.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            var pagedNews = newsItems
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var viewModel = new NewsIndexViewModel
            {
                News = pagedNews,
                CurrentPage = page,
                TotalPages = totalPages,
                CurrentSearch = searchTitle,
                CurrentSort = sortOrder
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Content,PublishDate")] News news, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                    var extension = Path.GetExtension(imageFile.FileName);
                    var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    news.ImagePath = $"/images/{uniqueFileName}";
                }
                else
                {
                    ModelState.AddModelError("ImagePath", "Зображення є обов'язковим.");
                    return View(news);
                }

                await _newsRepository.AddAsync(news);
                return RedirectToAction(nameof(Index));
            }

            return View(news);
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
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
            return View(news);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, News news, IFormFile imageFile)
        {
            if (id != news.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    Console.WriteLine($"ModelState Error: {error}");
                }
            }

            try
            {
                var existingNews = await _context.News.FindAsync(id);
                if (existingNews == null)
                {
                    return NotFound();
                }

                existingNews.Title = news.Title;
                existingNews.Content = news.Content;
                existingNews.PublishDate = news.PublishDate;

                if (imageFile != null && imageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingNews.ImagePath))
                    {
                        var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath,
                            existingNews.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    Directory.CreateDirectory(uploadsFolder);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    existingNews.ImagePath = "/images/" + uniqueFileName;
                }

                _context.Update(existingNews);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating news: {ex.Message}");
                ModelState.AddModelError("", "Виникла помилка при оновленні новини.");
                return View(news);
            }
        }

        public async Task<IActionResult> Delete(int? id)
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

            return View(news);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _newsRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

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

            return View(news);
        }

        public async Task<IActionResult> LatestNews(int count = 5)
        {
            var latestNews = await _newsRepository.GetLatestNewsAsync(count);
            return View(latestNews);
        }
    }
}
