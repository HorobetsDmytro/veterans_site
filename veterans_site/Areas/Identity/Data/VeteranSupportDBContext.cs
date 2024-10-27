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
    public DbSet<EventParticipant> EventParticipants { get; set; }
    public DbSet<ConsultationBooking> ConsultationBookings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<EventParticipant>()
            .HasOne(ep => ep.Event)
            .WithMany(e => e.EventParticipants)
            .HasForeignKey(ep => ep.EventId);

        builder.Entity<EventParticipant>()
            .HasOne(ep => ep.User)
            .WithMany()
            .HasForeignKey(ep => ep.UserId);

        builder.Entity<ConsultationBooking>()
            .HasOne(cb => cb.Consultation)
            .WithMany(c => c.Bookings)
            .HasForeignKey(cb => cb.ConsultationId);

        builder.Entity<ConsultationBooking>()
            .HasOne(cb => cb.User)
            .WithMany()
            .HasForeignKey(cb => cb.UserId);
    }
}
