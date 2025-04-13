using System;
using System.ComponentModel.DataAnnotations;

namespace veterans_site.Models
{
    public class AccessibilityMarker
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        public string Latitude { get; set; }
        
        [Required]
        public string Longitude { get; set; }
        
        public bool HasRamp { get; set; }
        public bool HasBlindSupport { get; set; }
        public bool HasElevator { get; set; }
        public bool HasAccessibleToilet { get; set; }
        public bool HasParking { get; set; }
        
        [StringLength(255)]
        public string Address { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public virtual ApplicationUser User { get; set; }
    }
}