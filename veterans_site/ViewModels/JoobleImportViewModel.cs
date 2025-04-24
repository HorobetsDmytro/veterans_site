using System.ComponentModel.DataAnnotations;

namespace veterans_site.ViewModels;

public class JoobleImportViewModel
{
    [Required(ErrorMessage = "Введіть ключові слова")]
    [Display(Name = "Ключові слова")]
    public string Keywords { get; set; }
    
    [Display(Name = "Місцезнаходження")]
    public string Location { get; set; }
    
    [Range(1, 100, ErrorMessage = "Кількість повинна бути від 1 до 100")]
    [Display(Name = "Кількість вакансій")]
    public int Count { get; set; }
}