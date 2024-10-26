using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.ViewModels;

namespace veterans_site.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ConsultationController : Controller
    {
        private readonly IConsultationRepository _consultationRepository;
        private const int PageSize = 6;

        public ConsultationController(IConsultationRepository consultationRepository)
        {
            _consultationRepository = consultationRepository;
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

            var consultation = await _consultationRepository.GetByIdAsync(id.Value);
            if (consultation == null)
                return NotFound();

            return View(consultation);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,DateTime,Duration,Type,Format,Status,Price,SpecialistName,MaxParticipants")] Consultation consultation)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                        }
                    }
                    return View(consultation);
                }

                if (consultation.MaxParticipants == null)
                {
                    consultation.MaxParticipants = 1;
                }

                await _consultationRepository.AddAsync(consultation);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating consultation: {ex.Message}");
                ModelState.AddModelError("", "Виникла помилка при створенні консультації. Спробуйте ще раз.");
                return View(consultation);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var consultation = await _consultationRepository.GetByIdAsync(id.Value);
            if (consultation == null)
                return NotFound();

            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,DateTime,Duration,Type,Format,Status,Price,SpecialistName,MaxParticipants")] Consultation consultation)
        {
            if (id != consultation.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _consultationRepository.UpdateAsync(consultation);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating consultation: {ex.Message}");
                    ModelState.AddModelError("", "Виникла помилка при оновленні консультації.");
                }
            }
            return View(consultation);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var consultation = await _consultationRepository.GetByIdAsync(id.Value);
            if (consultation == null)
                return NotFound();

            return View(consultation);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _consultationRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
