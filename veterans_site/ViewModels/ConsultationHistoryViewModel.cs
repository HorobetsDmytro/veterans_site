using veterans_site.Models;

namespace veterans_site.ViewModels
{
    public class ConsultationHistoryViewModel
    {
        public Consultation Consultation { get; set; }
        public DateTime BookingTime { get; set; }
        public ConsultationStatus Status { get; set; }
    }
}
