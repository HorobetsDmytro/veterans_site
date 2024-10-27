namespace veterans_site.ViewModels
{
    public class ConsultationBookingViewModel
    {
        public int ConsultationId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime ConsultationDateTime { get; set; }
        public string SpecialistName { get; set; }
        public double Price { get; set; }
        public string ConsultationType { get; set; }
    }
}
