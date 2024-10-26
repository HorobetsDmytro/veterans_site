using veterans_site.Models;

namespace veterans_site.ViewModels
{
    public class NewsDetailsViewModel
    {
        public News News { get; set; }
        public IEnumerable<News> RecentNews { get; set; }
    }
}
