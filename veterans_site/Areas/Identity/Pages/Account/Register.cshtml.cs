// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using veterans_site.Models;
using veterans_site.Services;

namespace veterans_site.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailService _emailService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailService emailService)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(50)]
            [Display(Name = "FirstName")]
            public string FirstName { get; set; }

            [Required]
            [StringLength(50)]
            [Display(Name = "LastName")]
            public string LastName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "Виберіть вашу роль")]
            [Display(Name = "Role")]
            public string SelectedRole { get; set; }

            [Display(Name = "Номер посвідчення ветерана")]
            [RegularExpression(@"^[А-ЯІЇЄҐ]{3}\s\d{6}$", ErrorMessage = "Введіть номер у форматі: ААА 123456")]
            public string? VeteranCertificateNumber { get; set; }

            [Display(Name = "Спеціалізація")]
            public string? SpecialistType { get; set; }

            [Display(Name = "Волонтерська організація")]
            [StringLength(100)]
            public string? VolunteerOrganization { get; set; }

            // Нові поля для ролі водія
            [Display(Name = "Модель автомобіля")]
            [StringLength(100)]
            public string? CarModel { get; set; }

            [Display(Name = "Номер автомобіля")]
            [RegularExpression(@"^[А-ЯІЇЄҐ]{2}\d{4}[А-ЯІЇЄҐ]{2}$", ErrorMessage = "Введіть номер у форматі: AA1234BB")]
            public string? LicencePlate { get; set; }

            [Display(Name = "Номер телефону")]
            [RegularExpression(@"^\+380\d{9}$", ErrorMessage = "Введіть номер у форматі: +380XXXXXXXXX")]
            public string? PhoneNumber { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public virtual async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ValidateRoleSpecificFields();

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    VeteranCertificateNumber = Input.SelectedRole == "Veteran" ? Input.VeteranCertificateNumber : null,
                    SpecialistType = Input.SelectedRole == "Specialist" ? Input.SpecialistType : null,
                    VolunteerOrganization = Input.SelectedRole == "Volunteer" ? Input.VolunteerOrganization : null,
                    CarModel = Input.SelectedRole == "Driver" ? Input.CarModel : null,
                    LicensePlate = Input.SelectedRole == "Driver" ? Input.LicencePlate : null
                };

                // Додаємо телефон окремо, оскільки це поле входить в базовий клас
                if (Input.SelectedRole == "Driver" && !string.IsNullOrEmpty(Input.PhoneNumber))
                {
                    user.PhoneNumber = Input.PhoneNumber;
                }

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Input.SelectedRole);

                    try
                    {
                        await _emailService.SendRegistrationConfirmationAsync(
                            user.Email,
                            $"{user.FirstName} {user.LastName}"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to send registration email: {ex.Message}");
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private void ValidateRoleSpecificFields()
        {
            if (Input.SelectedRole == "Veteran")
            {
                if (string.IsNullOrEmpty(Input.VeteranCertificateNumber))
                {
                    ModelState.AddModelError("Input.VeteranCertificateNumber", "Номер посвідчення ветерана обов'язковий для цієї ролі");
                }
                else if (!Regex.IsMatch(Input.VeteranCertificateNumber, @"^[А-ЯІЇЄҐ]{3}\s\d{6}$"))
                {
                    ModelState.AddModelError("Input.VeteranCertificateNumber", "Введіть номер у форматі: ААА 123456");
                }
            }
            else if (Input.SelectedRole == "Specialist")
            {
                if (string.IsNullOrEmpty(Input.SpecialistType))
                {
                    ModelState.AddModelError("Input.SpecialistType", "Виберіть спеціалізацію");
                }
            }
            else if (Input.SelectedRole == "Volunteer")
            {
                if (string.IsNullOrEmpty(Input.VolunteerOrganization))
                {
                    ModelState.AddModelError("Input.VolunteerOrganization", "Вкажіть назву волонтерської організації");
                }
            }
            else if (Input.SelectedRole == "Driver")
            {
                if (string.IsNullOrEmpty(Input.CarModel))
                {
                    ModelState.AddModelError("Input.CarModel", "Вкажіть модель автомобіля");
                }
        
                if (string.IsNullOrEmpty(Input.LicencePlate))
                {
                    ModelState.AddModelError("Input.CarNumber", "Вкажіть номер автомобіля");
                }
                else if (!Regex.IsMatch(Input.LicencePlate, @"^[А-ЯІЇЄҐ]{2}\d{4}[А-ЯІЇЄҐ]{2}$"))
                {
                    ModelState.AddModelError("Input.CarNumber", "Введіть номер у форматі: AA1234BB");
                }
        
                if (string.IsNullOrEmpty(Input.PhoneNumber))
                {
                    ModelState.AddModelError("Input.PhoneNumber", "Вкажіть номер телефону");
                }
                else if (!Regex.IsMatch(Input.PhoneNumber, @"^\+380\d{9}$"))
                {
                    ModelState.AddModelError("Input.PhoneNumber", "Введіть номер у форматі: +380XXXXXXXXX");
                }
            }
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}