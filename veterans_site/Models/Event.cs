using System.ComponentModel.DataAnnotations;

namespace veterans_site.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Location { get; set; }

        public int? MaxParticipants { get; set; }

        public EventStatus Status { get; set; }
        public EventCategory Category { get; set; }

    }

    public enum EventStatus
    {
        [Display(Name = "Заплановано")]
        Planned,    // Заплановано
        [Display(Name = "Проходить")]
        InProgress, // Проходить
        [Display(Name = "Завершено")]
        Completed,  // Завершено
        [Display(Name = "Скасовано")]
        Cancelled   // Скасовано
    }

    public enum EventCategory
    {
        [Display(Name = "Зустріч")]
        Meeting,        // Зустріч
        [Display(Name = "Тренінг")]
        Training,       // Тренінг
        [Display(Name = "Майстер-клас")]
        Workshop,       // Майстер-клас
        [Display(Name = "Консультація")]
        Consultation,   // Консультація
        [Display(Name = "Соціальний захід")]
        SocialEvent     // Соціальний захід
    }
}
