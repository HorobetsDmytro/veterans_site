using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.Data;
using veterans_site.ViewModels;

namespace veterans_site.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ConsultationController : Controller
    {
        private readonly IConsultationRepository _consultationRepository;
        private readonly VeteranSupportDBContext _context;
        private const int PageSize = 6;

        public ConsultationController(IConsultationRepository consultationRepository, VeteranSupportDBContext veteranSupportDBContext)
        {
            _consultationRepository = consultationRepository;
            _context = veteranSupportDBContext;
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
            var consultations = await _consultationRepository.GetFilteredConsultationsAsync(
                type, format, status, minPrice, maxPrice, sortOrder, page, PageSize);

            var totalPages = await _consultationRepository.GetTotalPagesAsync(
                type, format, status, minPrice, maxPrice, PageSize);

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

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var consultation = await _context.Consultations
                .Include(c => c.Slots)
                    .ThenInclude(s => s.User)
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consultation == null)
                return NotFound();

            return View(consultation);
        }
    }
}
