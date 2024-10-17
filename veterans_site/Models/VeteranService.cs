using System.ComponentModel.DataAnnotations;

namespace veterans_site.Models
{
    public class VeteranService
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        public string Category { get; set; } // Наприклад: освіта, пенсія, працевлаштування
    }
}
