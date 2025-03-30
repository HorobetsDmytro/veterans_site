using Microsoft.AspNetCore.Identity;
using veterans_site.Models;

namespace veterans_site.Middleware
{
    public class UserActivityCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public UserActivityCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null && !user.IsActive)
                {
                    var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
                    await signInManager.SignOutAsync();
                    context.Response.Redirect("/Identity/Account/Lockout");
                    return;
                }
            }

            await _next(context);
        }
    }

    public static class UserActivityCheckMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserActivityCheck(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserActivityCheckMiddleware>();
        }
    }
}
