using veterans_site.Models;

namespace veterans_site.ViewModels
{
    public class ConsultationIndexViewModel
    {
        public IEnumerable<Consultation> Consultations { get; set; }
        public ConsultationType? CurrentType { get; set; }
        public ConsultationFormat? CurrentFormat { get; set; }
        public ConsultationStatus? CurrentStatus { get; set; }
        public string CurrentSort { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
    }
}
