using System.ComponentModel.DataAnnotations;

namespace veterans_site.Models
{
    public class News
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime PublishDate { get; set; }

        // Зробіть поле ImagePath nullable
        public string? ImagePath { get; set; }
    }

}
