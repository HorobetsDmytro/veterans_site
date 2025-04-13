
namespace veterans_site.ViewModels
{
    public class MarkerDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public bool HasRamp { get; set; }
        public bool HasBlindSupport { get; set; }
        public bool HasElevator { get; set; }
        public bool HasAccessibleToilet { get; set; }
        public bool HasParking { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; }
    }
}