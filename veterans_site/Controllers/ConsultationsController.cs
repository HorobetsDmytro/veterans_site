using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using veterans_site.Extensions;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.Repositories;
using veterans_site.Services;
using veterans_site.ViewModels;

namespace veterans_site.Controllers
{
    public class ConsultationsController : Controller
    {
        private readonly IConsultationRepository _consultationRepository;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int PageSize = 6;

        public ConsultationsController(
            IConsultationRepository consultationRepository,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _consultationRepository = consultationRepository;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index(
            ConsultationType? type = null,
            ConsultationFormat? format = null,
            double? minPrice = null,
            double? maxPrice = null,
            string sortOrder = null,
            int page = 1)
        {
            var consultations = await _consultationRepository.GetAvailableConsultationsAsync(
                type, format, minPrice, maxPrice, sortOrder, page, PageSize);

            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                foreach (var consultation in consultations)
                {
                    consultation.IsBooked = await _consultationRepository.IsUserBookedForConsultationAsync(consultation.Id, userId);
                }
            }

            var totalPages = await _consultationRepository.GetTotalPagesAsync(
                type, format, null, minPrice, maxPrice, PageSize);

            var viewModel = new PublicConsultationIndexViewModel
            {
                Consultations = consultations,
                CurrentType = type,
                CurrentFormat = format,
                CurrentSort = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Book(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!user.IsActive)
            {
                TempData["Error"] = "Ваш обліковий запис неактивний. Ви не можете записуватись на консультації.";
                return RedirectToAction("Index");
            }

            var consultation = await _consultationRepository.GetByIdAsync(id);
            if (consultation == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (await _consultationRepository.IsUserBookedForConsultationAsync(id, userId))
            {
                TempData["Error"] = "Ви вже записані на цю консультацію.";
                return RedirectToAction(nameof(Index));
            }

            if (consultation.Format == ConsultationFormat.Individual && consultation.IsBooked)
            {
                TempData["Error"] = "На жаль, ця консультація вже заброньована.";
                return RedirectToAction(nameof(Index));
            }
            else if (consultation.Format == ConsultationFormat.Group &&
                     consultation.BookedParticipants >= consultation.MaxParticipants)
            {
                TempData["Error"] = "На жаль, вже немає вільних місць.";
                return RedirectToAction(nameof(Index));
            }

            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookConfirm(int id)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            if (await _consultationRepository.IsUserBookedForConsultationAsync(id, userId))
            {
                TempData["Error"] = "Ви вже записані на цю консультацію.";
                return RedirectToAction(nameof(Index));
            }

            var consultation = await _consultationRepository.GetByIdAsync(id);
            if (consultation == null)
            {
                TempData["Error"] = "Консультацію не знайдено.";
                return RedirectToAction(nameof(Index));
            }

            if (await _consultationRepository.BookConsultationAsync(id, userId))
            {
                // Відправляємо email підтвердження
                try
                {
                    await _emailService.SendConsultationConfirmationAsync(
                        user.Email,
                        $"{user.FirstName} {user.LastName}",
                        consultation.Title,
                        consultation.DateTime
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending confirmation email: {ex.Message}");
                }

                TempData["Success"] = "Ви успішно записались на консультацію!";
            }
            else
            {
                TempData["Error"] = "На жаль, не вдалося записатися на консультацію.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var consultation = await _consultationRepository.GetByIdAsync(id.Value);
            if (consultation == null)
                return NotFound();

            // Перевіряємо статус бронювання для авторизованого користувача
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                consultation.IsBooked = await _consultationRepository.IsUserBookedForConsultationAsync(consultation.Id, userId);
            }

            return View(consultation);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int consultationId)
        {
            var userId = _userManager.GetUserId(User);
            var consultation = await _consultationRepository.GetByIdAsync(consultationId);

            if (consultation == null)
            {
                TempData["Error"] = "Консультацію не знайдено.";
                return RedirectToAction("Index", "Profile");
            }

            // Перевіряємо чи користувач записаний на цю консультацію
            if (!await _consultationRepository.IsUserBookedForConsultationAsync(consultationId, userId))
            {
                TempData["Error"] = "Ви не записані на цю консультацію.";
                return RedirectToAction("Index", "Profile");
            }

            if (consultation.Format == ConsultationFormat.Individual)
            {
                consultation.IsBooked = false;
                consultation.UserId = null;
            }
            else
            {
                consultation.BookedParticipants--;
                await _consultationRepository.RemoveBookingAsync(consultationId, userId);
            }

            await _consultationRepository.UpdateAsync(consultation);
            TempData["Success"] = "Запис на консультацію успішно скасовано.";
            return RedirectToAction("Index", "Profile");
        }

    }
}
