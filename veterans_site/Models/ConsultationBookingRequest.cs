using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace veterans_site.Models
{
    public class ConsultationBookingRequest
    {
        public int Id { get; set; }

        public int ConsultationId { get; set; }
        [ForeignKey("ConsultationId")]
        public Consultation Consultation { get; set; }

        public int? SlotId { get; set; }
        [ForeignKey("SlotId")]
        public ConsultationSlot Slot { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public DateTime RequestTime { get; set; }
        public string Token { get; set; }
        public bool? IsApproved { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}