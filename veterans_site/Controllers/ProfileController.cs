using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.ViewModels;

namespace veterans_site.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConsultationRepository _consultationRepository;
        private readonly IEventRepository _eventRepository;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            IConsultationRepository consultationRepository,
            IEventRepository eventRepository)
        {
            _userManager = userManager;
            _consultationRepository = consultationRepository;
            _eventRepository = eventRepository;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Отримуємо консультації користувача
            var userConsultations = await _consultationRepository.GetUserConsultationsAsync(user.Id);
            var currentDate = DateTime.Now;

            var upcomingConsultations = userConsultations
                .Where(c => c.DateTime > currentDate && c.Status != ConsultationStatus.Cancelled)
                .OrderBy(c => c.DateTime);

            var pastConsultations = userConsultations
                .Where(c => c.DateTime <= currentDate || c.Status == ConsultationStatus.Cancelled)
                .OrderByDescending(c => c.DateTime);

            // Отримуємо події користувача
            var userEvents = await _eventRepository.GetUserEventsAsync(user.Id);

            var upcomingEvents = userEvents
                .Where(e => e.Date > currentDate && e.Status != EventStatus.Cancelled)
                .OrderBy(e => e.Date);

            var pastEvents = userEvents
                .Where(e => e.Date <= currentDate || e.Status == EventStatus.Cancelled)
                .OrderByDescending(e => e.Date);

            var viewModel = new UserProfileViewModel
            {
                User = user,
                UpcomingConsultations = upcomingConsultations,
                PastConsultations = pastConsultations,
                UpcomingEvents = upcomingEvents,
                PastEvents = pastEvents,
                TotalConsultations = userConsultations.Count(),
                TotalEvents = userEvents.Count()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> ConsultationHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var consultations = await _consultationRepository.GetUserConsultationsAsync(user.Id);
            return View(consultations.OrderByDescending(c => c.DateTime));
        }

        public async Task<IActionResult> EventHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var events = await _eventRepository.GetUserEventsAsync(user.Id);
            return View(events.OrderByDescending(e => e.Date));
        }

        [HttpPost]
        public async Task<IActionResult> CancelConsultation(int consultationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var consultation = await _consultationRepository.GetByIdAsync(consultationId);
            if (consultation == null || consultation.UserId != user.Id)
            {
                return NotFound();
            }

            // Перевіряємо, чи можна скасувати консультацію
            if (consultation.DateTime <= DateTime.Now)
            {
                TempData["Error"] = "Не можна скасувати консультацію, яка вже відбулась або триває.";
                return RedirectToAction(nameof(Index));
            }

            // Скасовуємо консультацію
            if (consultation.Format == ConsultationFormat.Group)
            {
                consultation.BookedParticipants--;
            }
            consultation.Status = ConsultationStatus.Cancelled;
            consultation.UserId = null;
            consultation.IsBooked = false;

            await _consultationRepository.UpdateAsync(consultation);

            TempData["Success"] = "Консультацію успішно скасовано.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ConsultationDetails(int id)
        {
            var userId = _userManager.GetUserId(User);
            var consultation = await _consultationRepository.GetByIdAsync(id);

            if (consultation == null)
            {
                return NotFound();
            }

            if (!await _consultationRepository.IsUserBookedForConsultationAsync(id, userId))
            {
                return Forbid();
            }

            return View(consultation);
        }

        //public async Task<IActionResult> EventDetails(int id)
        //{
        //    var userId = _userManager.GetUserId(User);
        //    var evt = await _eventRepository.GetByIdWithParticipantsAsync(id);

        //    if (evt == null)
        //    {
        //        return NotFound();
        //    }

        //    // Перевіряємо чи користувач зареєстрований на цю подію
        //    if (!evt.EventParticipants.Any(p => p.UserId == userId))
        //    {
        //        return Forbid();
        //    }

        //    return View(evt);
        //}
    }
}
