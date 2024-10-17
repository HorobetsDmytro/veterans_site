using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using veterans_site.Models;

namespace veterans_site.Data;

public class VeteranSupportDBContext : IdentityDbContext<ApplicationUser>
{   
    public VeteranSupportDBContext(DbContextOptions<VeteranSupportDBContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events { get; set; }
    public DbSet<Consultation> Consultations { get; set; }
    public DbSet<News> News { get; set; }
    public DbSet<VeteranService> VeteranServices { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }
}
