﻿using Microsoft.AspNetCore.Identity;
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

        public bool IsActive { get; set; } = true;  // За замовчуванням користувач активний

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        // Навігаційні властивості
        public virtual ICollection<ConsultationBooking> ConsultationBookings { get; set; }
        public virtual ICollection<EventParticipant> EventParticipants { get; set; }
    }
}
