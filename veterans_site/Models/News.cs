using System.ComponentModel.DataAnnotations;

namespace veterans_site.Models
{
    public class News
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public DateTime PublishDate { get; set; }

        public string Author { get; set; }
    }
}
