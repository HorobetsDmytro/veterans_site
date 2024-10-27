using veterans_site.Models;

namespace veterans_site.ViewModels
{
    public class UserProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public IEnumerable<Consultation> UpcomingConsultations { get; set; }
        public IEnumerable<Consultation> PastConsultations { get; set; }
        public IEnumerable<Event> UpcomingEvents { get; set; }
        public IEnumerable<Event> PastEvents { get; set; }

        // Додаткова статистика
        public int TotalConsultations { get; set; }
        public int TotalEvents { get; set; }
    }
}
