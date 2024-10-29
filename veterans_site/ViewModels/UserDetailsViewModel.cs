using veterans_site.Models;

namespace veterans_site.ViewModels
{
    public class UserDetailsViewModel
    {
        public ApplicationUser User { get; set; }
        public IEnumerable<Consultation> UpcomingConsultations { get; set; }
        public IEnumerable<Consultation> PastConsultations { get; set; }
        public IEnumerable<Event> UpcomingEvents { get; set; }
        public IEnumerable<Event> PastEvents { get; set; }
        public List<string> Roles { get; set; }
    }
}
