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
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<UserConnection> UsersConnections { get; set; }
    public DbSet<GeneralChatMessage> GeneralChatMessages { get; set; }
    public DbSet<UserLastReadGeneralChat> UserLastReadGeneralChats { get; set; }
    public DbSet<AccessibilityMarker> AccessibilityMarkers { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }
    public DbSet<Resume> Resumes { get; set; }
    public DbSet<SavedJob> SavedJobs { get; set; }
    public DbSet<TaxiRide> TaxiRides { get; set; }
        
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

        builder.Entity<ChatMessage>()
            .HasOne(cm => cm.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(cm => cm.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserConnection>()
            .HasNoKey();
        
        builder.Entity<TaxiRide>()
            .HasOne(r => r.Veteran)
            .WithMany()
            .HasForeignKey(r => r.VeteranId)
            .OnDelete(DeleteBehavior.Restrict);
                
        builder.Entity<TaxiRide>()
            .HasOne(r => r.Driver)
            .WithMany(d => d.Rides)
            .HasForeignKey(r => r.DriverId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    // private void SeedTaxiDrivers(ModelBuilder builder)
    // {
    //     builder.Entity<TaxiDriver>().HasData(
    //         new TaxiDriver
    //         {
    //             Id = "driver1",
    //             Name = "Олександр Петренко",
    //             PhoneNumber = "+380991234567",
    //             CarModel = "Toyota Camry",
    //             LicensePlate = "АА1234ВВ",
    //             PhotoUrl = "/images/drivers/driver1.jpg",
    //             Rating = 4.8,
    //             CurrentLatitude = 50.4501,
    //             CurrentLongitude = 30.5234,
    //             IsAvailable = true
    //         },
    //         new TaxiDriver
    //         {
    //             Id = "driver2",
    //             Name = "Сергій Коваленко",
    //             PhoneNumber = "+380992345678",
    //             CarModel = "Hyundai Sonata",
    //             LicensePlate = "АА5678ВС",
    //             PhotoUrl = "/images/drivers/driver2.jpg",
    //             Rating = 4.7,
    //             CurrentLatitude = 50.4520,
    //             CurrentLongitude = 30.5300,
    //             IsAvailable = true
    //         },
    //         new TaxiDriver
    //         {
    //             Id = "driver3",
    //             Name = "Іван Мельник",
    //             PhoneNumber = "+380993456789",
    //             CarModel = "Volkswagen Passat",
    //             LicensePlate = "АА9012ВТ",
    //             PhotoUrl = "/images/drivers/driver3.jpg",
    //             Rating = 4.9,
    //             CurrentLatitude = 50.4470,
    //             CurrentLongitude = 30.5180,
    //             IsAvailable = true
    //         }
    //     );
    // }
}
