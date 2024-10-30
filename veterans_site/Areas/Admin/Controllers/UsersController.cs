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
        private const int PageSize = 10;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            IConsultationRepository consultationRepository,
            IEventRepository eventRepository,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _consultationRepository = consultationRepository;
            _eventRepository = eventRepository;
            _roleManager = roleManager;
            _emailService = emailService;
            _logger = logger;
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

            // Перевіряємо чи не намагаємось видалити адміна
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Неможливо видалити адміністратора.";
                return RedirectToAction(nameof(Index));
            }

            // Видаляємо всі бронювання користувача
            await _consultationRepository.RemoveUserBookingsAsync(id);
            await _eventRepository.RemoveUserParticipationsAsync(id);

            // Видаляємо користувача
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
            var allRoles = new List<string> { "Veteran", "Specialist" }; // Фіксований список доступних ролей

            var model = new UserRolesViewModel
            {
                UserId = userId,
                UserName = $"{user.FirstName} {user.LastName}",
                SelectedRole = userRoles.FirstOrDefault(), // Поточна роль
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

            try
            {
                // Отримуємо поточні ролі користувача
                var userRoles = await _userManager.GetRolesAsync(user);
                var currentRole = userRoles.FirstOrDefault();

                // Якщо нова роль відрізняється від поточної
                if (currentRole != model.SelectedRole)
                {
                    // Видаляємо всі поточні ролі
                    if (userRoles.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, userRoles);
                    }

                    // Додаємо нову роль
                    if (!string.IsNullOrEmpty(model.SelectedRole))
                    {
                        await _userManager.AddToRoleAsync(user, model.SelectedRole);

                        // Відправляємо email про зміну ролі
                        try
                        {
                            await _emailService.SendRoleChangedEmailAsync(
                                user.Email,
                                $"{user.FirstName} {user.LastName}",
                                model.SelectedRole
                            );  
                        }
                        catch (Exception emailEx)
                        {
                            // Логуємо помилку, але не перериваємо процес
                            _logger.LogError($"Failed to send role change email: {emailEx.Message}");
                        }
                    }

                    TempData["Success"] = "Роль користувача успішно оновлено";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Помилка при оновленні ролі: {ex.Message}";
                _logger.LogError($"Error updating user role: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
