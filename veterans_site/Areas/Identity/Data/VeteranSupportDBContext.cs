using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using veterans_site.Models;

namespace veterans_site.Data;

public class VeteranSupportDbContext : IdentityDbContext<ApplicationUser>
{
    public VeteranSupportDbContext(DbContextOptions<VeteranSupportDbContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events { get; set; }
    public DbSet<Consultation> Consultations { get; set; }
    public DbSet<News> News { get; set; }
    public DbSet<VeteranService> VeteranServices { get; set; }
    public DbSet<EventParticipant> EventParticipants { get; set; }
    public DbSet<ConsultationBooking> ConsultationBookings { get; set; }
    public DbSet<RoleChangeRequest> RoleChangeRequests { get; set; }
    public DbSet<ConsultationSlot> ConsultationSlots { get; set; }
    public DbSet<ConsultationBookingRequest> ConsultationBookingRequests { get; set; }
    public DbSet<EventComment> EventComments { get; set; }


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
            .HasForeignKey(cb => cb.ConsultationId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.Entity<ConsultationBooking>()
            .HasOne(cb => cb.User)
            .WithMany()
            .HasForeignKey(cb => cb.UserId);

        builder.Entity<ApplicationUser>()
            .Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Entity<ApplicationUser>()
            .Property(u => u.RegistrationDate)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Entity<ConsultationSlot>()
            .HasOne(cs => cs.Consultation)
            .WithMany(c => c.Slots)
            .HasForeignKey(cs => cs.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ConsultationSlot>()
            .HasOne(cs => cs.User)
            .WithMany()
            .HasForeignKey(cs => cs.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ConsultationBookingRequest>()
            .HasOne(r => r.Consultation)
            .WithMany()
            .HasForeignKey(r => r.ConsultationId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.Entity<ConsultationBookingRequest>()
            .HasOne(r => r.Slot)
            .WithMany()
            .HasForeignKey(r => r.SlotId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.Entity<ConsultationBookingRequest>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId);
    }
}
