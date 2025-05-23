using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using veterans_site.Areas.Admin.Controllers;
using veterans_site.Data;
using veterans_site.Hubs;
using veterans_site.Interfaces;
using veterans_site.Middleware;
using veterans_site.Models;
using veterans_site.Repositories;
using veterans_site.Services;
using ConsultationController = veterans_site.Areas.Specialist.Controllers.ConsultationController;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<VeteranSupportDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<VeteranSupportDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
});

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>>();

builder.Services.AddSignalR(options => {
    options.EnableDetailedErrors = true;
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(1);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.Configure<IdentityOptions>(options => { });
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, CustomUserClaimsPrincipalFactory>();
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<INewsRepository, NewsRepository>();
builder.Services.AddScoped<IConsultationRepository, ConsultationRepository>();
builder.Services.AddScoped<IAccessibilityMarkerRepository, AccessibilityMarkerRepository>();
builder.Services.AddScoped<ILogger<ConsultationController>, Logger<ConsultationController>>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<IResumeRepository, ResumeRepository>();
builder.Services.AddScoped<ISavedJobRepository, SavedJobRepository>();
builder.Services.AddScoped<ISocialTaxiRepository, SocialTaxiRepository>();

builder.Services.AddScoped<GoogleCalendarService>();
builder.Services.AddScoped<IPDFService, PDFService>();
builder.Services.AddHttpClient<IJoobleService, JoobleService>();
builder.Services.AddScoped<IJoobleService, JoobleService>();
builder.Services.AddHttpClient<UberApiService>();

builder.Services.AddDataProtection();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddControllersWithViews();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
    .AddPolicy("RequireVeteranRole", policy => policy.RequireRole("Veteran"))
    .AddPolicy("RequireSpecialistRole", policy => policy.RequireRole("Specialist"))
    .AddPolicy("RequireDriverRole", policy => policy.RequireRole("Driver"));

builder.Services.AddHostedService<ConsultationBackgroundService>();
builder.Services.AddHostedService<EventBackgroundService>();
builder.Services.AddHostedService<JoobleBackgroundService>();
builder.Services.AddHostedService<ScheduledRidesService>();
builder.Services.AddHostedService<ScheduledRidesWorker>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanRegisterForEvents", policy =>
        policy.RequireRole("Veteran"));
});

var app = builder.Build();
    
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var rolesManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await RoleInitializer.InitializeAsync(userManager, rolesManager);

        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "GoogleCalendarTokens")))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "GoogleCalendarTokens"));
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseUserActivityCheck();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapDefaultControllerRoute();
app.MapRazorPages();

app.MapHub<ChatHub>("/chatHub");
app.MapHub<TaxiHub>("/taxiHub");

app.Run();