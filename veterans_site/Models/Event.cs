using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace veterans_site.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Назва")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Опис")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Дата")]
        public DateTime Date { get; set; }

        [Required]
        [Display(Name = "Місце проведення")]
        public string Location { get; set; }

        [Display(Name = "Максимум учасників")]
        public int? MaxParticipants { get; set; }

        [Required]
        [Display(Name = "Статус")]
        public EventStatus Status { get; set; }

        [Required]
        [Display(Name = "Категорія")]
        public EventCategory Category { get; set; }

        [Required]
        [Range(10, 480, ErrorMessage = "Тривалість повинна бути від 10 до 480 хвилин")]
        [Display(Name = "Тривалість (хв)")]
        public int Duration { get; set; }

        public ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();

        [NotMapped]
        public int AvailableSpots => MaxParticipants.HasValue
            ? MaxParticipants.Value - EventParticipants.Count
            : int.MaxValue;

        [NotMapped]
        public bool CanRegister => !MaxParticipants.HasValue || EventParticipants.Count < MaxParticipants.Value;

        [NotMapped]
        public DateTime EndTime => Date.AddMinutes(Duration);
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
