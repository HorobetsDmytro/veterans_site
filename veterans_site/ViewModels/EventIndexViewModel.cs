using veterans_site.Models;

namespace veterans_site.ViewModels
{
    public class EventIndexViewModel
    {
        public List<Event> Events { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public EventCategory? CurrentCategory { get; set; }
        public EventStatus? CurrentStatus { get; set; }
        public string CurrentSort { get; set; }
    }
}
