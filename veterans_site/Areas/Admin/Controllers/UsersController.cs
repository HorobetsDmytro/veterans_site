using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using veterans_site.Models;
using veterans_site.ViewModels;
using veterans_site.Interfaces;
using veterans_site.Services;
using veterans_site.Data;

namespace veterans_site.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly IConsultationRepository _consultationRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ILogger<UsersController> _logger;
        private readonly VeteranSupportDBContext _context;
        private readonly IConfiguration _configuration;
        private const int PageSize = 10;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            IConsultationRepository consultationRepository,
            IEventRepository eventRepository,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            ILogger<UsersController> logger,
            VeteranSupportDBContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _consultationRepository = consultationRepository;
            _eventRepository = eventRepository;
            _roleManager = roleManager;
            _emailService = emailService;
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CurrentFilter"] = searchString ?? currentFilter;

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserManagementViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var consultationsCount = await _consultationRepository.GetUserConsultationsCount(user.Id);
                var eventsCount = await _eventRepository.GetUserEventsCount(user.Id);

                userViewModels.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RegistrationDate = user.RegistrationDate,
                    IsActive = user.IsActive,
                    Roles = roles.ToList(),
                    ConsultationsCount = consultationsCount,
                    EventsCount = eventsCount
                });
            }

            // Застосовуємо пошук
            if (!String.IsNullOrEmpty(searchString))
            {
                userViewModels = userViewModels.Where(u =>
                    u.Email.Contains(searchString) ||
                    u.FirstName.Contains(searchString) ||
                    u.LastName.Contains(searchString)).ToList();
            }

            // Застосовуємо сортування
            userViewModels = sortOrder switch
            {
                "name_desc" => userViewModels.OrderByDescending(u => u.LastName).ToList(),
                "Date" => userViewModels.OrderBy(u => u.RegistrationDate).ToList(),
                "date_desc" => userViewModels.OrderByDescending(u => u.RegistrationDate).ToList(),
                _ => userViewModels.OrderBy(u => u.LastName).ToList(),
            };

            int pageNumber = (page ?? 1);
            var totalPages = (int)Math.Ceiling(userViewModels.Count() / (double)PageSize);
            var pagedUsers = userViewModels.Skip((pageNumber - 1) * PageSize).Take(PageSize);

            var viewModel = new UserIndexViewModel
            {
                Users = pagedUsers,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                CurrentSort = sortOrder,
                CurrentFilter = searchString
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var consultations = await _consultationRepository.GetUserConsultationsAsync(id);
            var events = await _eventRepository.GetUserEventsAsync(id);
            var currentDate = DateTime.Now;

            var viewModel = new UserDetailsViewModel
            {
                User = user,
                Roles = roles.ToList(),
                UpcomingConsultations = consultations.Where(c => c.DateTime > currentDate),
                PastConsultations = consultations.Where(c => c.DateTime <= currentDate),
                UpcomingEvents = events.Where(e => e.Date > currentDate),
                PastEvents = events.Where(e => e.Date <= currentDate)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Неможливо видалити адміністратора.";
                return RedirectToAction(nameof(Index));
            }

            await _consultationRepository.RemoveUserBookingsAsync(id);
            await _eventRepository.RemoveUserParticipationsAsync(id);

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Користувача успішно видалено.";
            }
            else
            {
                TempData["Error"] = "Виникла помилка при видаленні користувача.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = user.IsActive ?
                "Обліковий запис активовано." :
                "Обліковий запис деактивовано.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = new List<string> { "Veteran", "Specialist" };

            var model = new UserRolesViewModel
            {
                UserId = userId,
                UserName = $"{user.FirstName} {user.LastName}",
                SelectedRole = userRoles.FirstOrDefault(),
                AvailableRoles = allRoles
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(UserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            if (currentRole == model.SelectedRole)
            {
                TempData["Error"] = $"Користувач вже має роль {model.SelectedRole}";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var token = Guid.NewGuid().ToString();

                var roleChangeRequest = new RoleChangeRequest
                {
                    UserId = user.Id,
                    NewRole = model.SelectedRole,
                    Token = token,
                    ExpiryTime = DateTime.UtcNow.AddHours(24),
                    IsConfirmed = false
                };

                _context.RoleChangeRequests.Add(roleChangeRequest);
                await _context.SaveChangesAsync();

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var confirmationLink = $"{baseUrl}/Admin/Users/ConfirmRoleChange?token={token}&confirm=true";
                var rejectLink = $"{baseUrl}/Admin/Users/ConfirmRoleChange?token={token}&confirm=false";

                await _emailService.SendRoleChangeConfirmationEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    model.SelectedRole,
                    confirmationLink,
                    rejectLink
                );

                TempData["Success"] = "Запит на зміну ролі надіслано користувачу. Очікуйте підтвердження.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Помилка: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ConfirmRoleChange(string token, bool confirm)
        {
            if (string.IsNullOrEmpty(token))
            {
                return View("Error", new ErrorViewModel { Message = "Токен не може бути пустим." });
            }

            var request = await _context.RoleChangeRequests
                .FirstOrDefaultAsync(r => r.Token == token && !r.IsConfirmed && r.ExpiryTime > DateTime.UtcNow);

            if (request == null)
            {
                return View("Error", new ErrorViewModel { Message = "Недійсний або застарілий токен." });
            }

            try
            {
                if (confirm)
                {
                    var user = await _userManager.FindByIdAsync(request.UserId);
                    if (user != null)
                    {
                        var currentRoles = await _userManager.GetRolesAsync(user);
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRoleAsync(user, request.NewRole);

                        request.IsConfirmed = true;
                        await _context.SaveChangesAsync();

                        return View("RoleChangeSuccess");
                    }
                }
                else
                {
                    _context.RoleChangeRequests.Remove(request);
                    await _context.SaveChangesAsync();

                    return View("RoleChangeRejected");
                }
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Message = $"Помилка при зміні ролі: {ex.Message}" });
            }

            return View("Error", new ErrorViewModel { Message = "Щось пішло не так." });
        }
    }
}
