namespace veterans_site.Models
{
    public class ConsultationBooking
    {
        public int Id { get; set; }

        public int ConsultationId { get; set; }
        public Consultation Consultation { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime BookingTime { get; set; }
    }
}
