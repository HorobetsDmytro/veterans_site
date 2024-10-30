﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace veterans_site.Models
{
    public class Consultation
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Поле Назва є обов'язковим")]
        [StringLength(100)]
        [Display(Name = "Назва")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Поле Опис є обов'язковим")]
        [Display(Name = "Опис")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Поле Дата та час є обов'язковим")]
        [Display(Name = "Дата та час")]
        public DateTime DateTime { get; set; }

        [Required(ErrorMessage = "Поле Тривалість є обов'язковим")]
        [Range(1, 480, ErrorMessage = "Тривалість повинна бути від 1 до 480 хвилин")]
        [Display(Name = "Тривалість (хв)")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Поле Тип консультації є обов'язковим")]
        [Display(Name = "Тип консультації")]
        public ConsultationType Type { get; set; }

        [Required(ErrorMessage = "Поле Формат консультації є обов'язковим")]
        [Display(Name = "Формат консультації")]
        public ConsultationFormat Format { get; set; }

        [Required(ErrorMessage = "Поле Статус є обов'язковим")]
        [Display(Name = "Статус")]
        public ConsultationStatus Status { get; set; }

        [Required(ErrorMessage = "Поле Ціна є обов'язковим")]
        [Range(0, double.MaxValue, ErrorMessage = "Ціна повинна бути більше 0")]
        [Display(Name = "Ціна")]
        public double Price { get; set; }

        [Required(ErrorMessage = "Поле Ім'я спеціаліста є обов'язковим")]
        [Display(Name = "Ім'я спеціаліста")]
        public string SpecialistName { get; set; }

        [Display(Name = "Максимум учасників")]
        public int? MaxParticipants { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public DateTime? BookingTime { get; set; }

        public bool IsBooked { get; set; } = false;

        [Display(Name = "Кількість зареєстрованих учасників")]
        public int BookedParticipants { get; set; } = 0;

        public ICollection<ConsultationBooking> Bookings { get; set; } = new List<ConsultationBooking>();

        [Required(ErrorMessage = "Оберіть формат проведення консультації")]
        [Display(Name = "Формат проведення")]
        public ConsultationMode Mode { get; set; }

        [Display(Name = "Місце проведення")]
        public string Location { get; set; }

        // Валідація для місця проведення, якщо консультація офлайн
        public bool ShouldValidateLocation()
        {
            return Mode == ConsultationMode.Offline;
        }
    }

    public enum ConsultationType
    {
        [Display(Name = "Медична")]
        Medical,
        [Display(Name = "Психологічна")]
        Psychological,
        [Display(Name = "Юридична")]
        Legal
    }

    public enum ConsultationFormat
    {
        [Display(Name = "Індивідуальна")]
        Individual,
        [Display(Name = "Групова")]
        Group
    }

    public enum ConsultationStatus
    {
        [Display(Name = "Заплановано")]
        Planned,
        [Display(Name = "Проходить")]
        InProgress,
        [Display(Name = "Завершено")]
        Completed,
        [Display(Name = "Скасовано")]
        Cancelled
    }

    public enum ConsultationMode
    {
        [Display(Name = "Онлайн")]
        Online,
        [Display(Name = "Офлайн")]
        Offline
    }
}
