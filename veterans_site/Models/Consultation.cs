using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace veterans_site.Models
{
    public class Consultation
    {
        public int Id { get; set; }

        [Required]
        public ConsultationType Type { get; set; } // Медична, психологічна, юридична

        [Required]
        public DateTime DateTime { get; set; }

        [Required]
        public string SpecialistName { get; set; }

        public string Description { get; set; }

        [ForeignKey("Veteran")]
        public string VeteranId { get; set; }
        public ApplicationUser Veteran { get; set; }
    }

    public enum ConsultationType
    {
        Medical,
        psychological,
        Legal
    }
}
