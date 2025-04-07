using System.ComponentModel.DataAnnotations;

namespace veterans_site.ViewModels;

public class EditAccountViewModel
{
    [Required(ErrorMessage = "Введіть ім'я")]
    [Display(Name = "Ім'я")]
    [StringLength(50, ErrorMessage = "Ім'я не може бути довшим за 50 символів")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Введіть прізвище")]
    [Display(Name = "Прізвище")]
    [StringLength(50, ErrorMessage = "Прізвище не може бути довшим за 50 символів")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Введіть електронну пошту")]
    [EmailAddress(ErrorMessage = "Неправильний формат електронної пошти")]
    [Display(Name = "Електронна пошта")]
    public string Email { get; set; }
}