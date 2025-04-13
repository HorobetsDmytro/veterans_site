using System.ComponentModel.DataAnnotations;

namespace veterans_site.ViewModels
{
    public class AddMarkerViewModel
    {
        [Required(ErrorMessage = "Назва місця обов'язкова")]
        [StringLength(100, ErrorMessage = "Назва не повинна перевищувати 100 символів")]
        public string Title { get; set; }
        
        [StringLength(255, ErrorMessage = "Адреса не повинна перевищувати 255 символів")]
        public string Address { get; set; }
        
        [StringLength(500, ErrorMessage = "Опис не повинен перевищувати 500 символів")]
        public string Description { get; set; }
        
        [Required(ErrorMessage = "Широта обов'язкова")]
        [StringLength(20, ErrorMessage = "Широта не повинна перевищувати 20 символів")]
        public string Latitude { get; set; }

        [Required(ErrorMessage = "Довгота обов'язкова")]
        [StringLength(20, ErrorMessage = "Довгота не повинна перевищувати 20 символів")]
        public string Longitude { get; set; }
        
        public bool HasRamp { get; set; }
        public bool HasBlindSupport { get; set; }
        public bool HasElevator { get; set; }
        public bool HasAccessibleToilet { get; set; }
        public bool HasParking { get; set; }
    }
}