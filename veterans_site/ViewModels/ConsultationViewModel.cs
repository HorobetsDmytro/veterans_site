using veterans_site.Models;

namespace veterans_site.ViewModels
{
    public class ConsultationViewModel
    {
        public Consultation Consultation { get; set; }
        public List<TimeSlot> AvailableSlots { get; set; }
    }
}
