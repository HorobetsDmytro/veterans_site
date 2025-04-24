using System.ComponentModel.DataAnnotations;

namespace veterans_site.ViewModels;

public class ResumeViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Введіть ваше повне ім'я")]
    [Display(Name = "Повне ім'я")]
    public string? FullName { get; set; }
    
    [EmailAddress(ErrorMessage = "Введіть коректну електронну адресу")]
    [Display(Name = "Email")]
    public string? Email { get; set; }
    
    [Phone(ErrorMessage = "Введіть коректний номер телефону")]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }
    
    [Display(Name = "Навички")]
    public string? Skills { get; set; }
    
    [Display(Name = "Досвід роботи")]
    [DataType(DataType.MultilineText)]
    public string? Experience { get; set; }
    
    [Display(Name = "Освіта")]
    [DataType(DataType.MultilineText)]
    public string? Education { get; set; }
    
    [Display(Name = "Додаткова інформація")]
    [DataType(DataType.MultilineText)]
    public string? AdditionalInfo { get; set; }
    
    [Display(Name = "Файл резюме (PDF)")]
    public IFormFile? ResumeFile { get; set; }
    
    public string? ExistingFilePath { get; set; }
    public string ResumeInputType { get; set; } = "manual";
    public string OriginalResumeInputType { get; set; }
}