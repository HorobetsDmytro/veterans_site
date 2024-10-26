using System.Collections.Generic;
using veterans_site.Models;

namespace veterans_site.ViewModels
{
    public class NewsIndexViewModel
    {
        public IEnumerable<News> News { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string CurrentSearch { get; set; }
        public string CurrentSort { get; set; }
    }
}
