using System.ComponentModel.DataAnnotations.Schema;

namespace veterans_site.Models
{
    public class ConsultationSlot
    {
        public int Id { get; set; }

        public int ConsultationId { get; set; }
        public Consultation Consultation { get; set; }

        public DateTime DateTime { get; set; }

        public bool IsBooked { get; set; }
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}
