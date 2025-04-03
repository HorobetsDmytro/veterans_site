using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace veterans_site.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public string? AvatarPath { get; set; }
        
        public bool IsOnline { get; set; } = false;
        public DateTime LastOnline { get; set; } = DateTime.Now;

        public virtual ICollection<ConsultationBooking> ConsultationBookings { get; set; }
        public virtual ICollection<EventParticipant> EventParticipants { get; set; }
        public virtual ICollection<ChatMessage> SentMessages { get; set; }
        public virtual ICollection<ChatMessage> ReceivedMessages { get; set; }
    }
}
