using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using veterans_site.Models;

namespace veterans_site.Services
{
    public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
            _userManager = userManager;
        }

        public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var principal = await base.CreateAsync(user);

            if (!await _userManager.IsInRoleAsync(user, "Admin") &&
                !await _userManager.IsInRoleAsync(user, "Specialist") &&
                !await _userManager.IsInRoleAsync(user, "Veteran"))
            {
                await _userManager.AddToRoleAsync(user, "Veteran");
            }

            return principal;
        }
    }
}
