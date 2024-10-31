using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.ViewModels;

namespace veterans_site.Areas.Specialist.Controllers
{
    [Area("Specialist")]
    [Authorize(Roles = "Specialist")]
    public class ConsultationController : Controller
    {
        private readonly IConsultationRepository _consultationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int PageSize = 6;

        public ConsultationController(IConsultationRepository consultationRepository, UserManager<ApplicationUser> userManager)
        {
            _consultationRepository = consultationRepository;
            _userManager = userManager;
        }

        // Areas/Specialist/Controllers/ConsultationController.cs

        public async Task<IActionResult> Index(
            ConsultationType? type = null,
            ConsultationFormat? format = null,
            ConsultationStatus? status = null,
            string sortOrder = null,
            int page = 1)
        {
            // Отримуємо поточного користувача
            var currentUser = await _userManager.GetUserAsync(User);
            var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

            // Отримуємо консультації тільки для цього спеціаліста
            var consultations = await _consultationRepository.GetFilteredConsultationsAsync(
                type, format, status, null, null, sortOrder, page, PageSize, specialistName); // Додаємо specialistName як параметр

            var totalPages = await _consultationRepository.GetTotalPagesAsync(
                type, format, status, null, null, PageSize, specialistName); // Додаємо specialistName як параметр

            var viewModel = new ConsultationIndexViewModel
            {
                Consultations = consultations,
                CurrentType = type,
                CurrentFormat = format,
                CurrentStatus = status,
                CurrentSort = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        public IActionResult Create()
        {
            // Отримуємо ім'я поточного спеціаліста
            var currentUser = _userManager.GetUserAsync(User).Result;
            var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

            var consultation = new Consultation
            {
                SpecialistName = specialistName,
                Status = ConsultationStatus.Planned,
                DateTime = DateTime.Now.AddDays(1) // За замовчуванням встановлюємо на завтра
            };

            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Consultation consultation)
        {
            // Перевіряємо місце проведення тільки для офлайн консультацій
            if (consultation.Mode == ConsultationMode.Offline && string.IsNullOrWhiteSpace(consultation.Location))
            {
                ModelState.AddModelError("Location", "Для офлайн консультації необхідно вказати місце проведення");
            }
            else if (consultation.Mode == ConsultationMode.Online)
            {
                consultation.Location = null; // Очищаємо місце проведення для онлайн консультацій
            }

            if (ModelState.IsValid)
            {
                // Перевстановлюємо ім'я спеціаліста для безпеки
                var currentUser = await _userManager.GetUserAsync(User);
                consultation.SpecialistName = $"{currentUser.FirstName} {currentUser.LastName}";

                await _consultationRepository.AddAsync(consultation);
                return RedirectToAction(nameof(Index));
            }
            return View(consultation);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var consultation = await _consultationRepository.GetByIdAsync(id.Value);
            if (consultation == null)
                return NotFound();

            // Перевіряємо, чи належить консультація поточному спеціалісту
            var currentUser = await _userManager.GetUserAsync(User);
            var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

            if (consultation.SpecialistName != specialistName)
                return Forbid();

            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Consultation consultation)
        {
            if (id != consultation.Id)
                return NotFound();

            // Перевіряємо, чи належить консультація поточному спеціалісту
            var currentUser = await _userManager.GetUserAsync(User);
            var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

            if (consultation.SpecialistName != specialistName)
                return Forbid();

            if (consultation.Mode == ConsultationMode.Offline && string.IsNullOrWhiteSpace(consultation.Location))
            {
                ModelState.AddModelError("Location", "Для офлайн консультації необхідно вказати місце проведення");
            }

            if (ModelState.IsValid)
            {
                await _consultationRepository.UpdateAsync(consultation);
                return RedirectToAction(nameof(Index));
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

            // Перевіряємо, чи належить консультація поточному спеціалісту
            var currentUser = await _userManager.GetUserAsync(User);
            var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

            if (consultation.SpecialistName != specialistName)
                return Forbid();

            return View(consultation);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var consultation = await _consultationRepository.GetByIdAsync(id);
            if (consultation == null)
                return NotFound();

            // Перевіряємо, чи належить консультація поточному спеціалісту
            var currentUser = await _userManager.GetUserAsync(User);
            var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

            if (consultation.SpecialistName != specialistName)
                return Forbid();

            await _consultationRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var consultation = await _consultationRepository.GetByIdAsync(id.Value);
            if (consultation == null)
                return NotFound();

            // Перевіряємо, чи належить консультація поточному спеціалісту
            var currentUser = await _userManager.GetUserAsync(User);
            var specialistName = $"{currentUser.FirstName} {currentUser.LastName}";

            if (consultation.SpecialistName != specialistName)
                return Forbid();

            return View(consultation);
        }
    }
}
