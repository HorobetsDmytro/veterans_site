using System.ComponentModel.DataAnnotations;

namespace veterans_site.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Введіть поточний пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Поточний пароль")]
    public string CurrentPassword { get; set; }

    [Required(ErrorMessage = "Введіть новий пароль")]
    [StringLength(100, ErrorMessage = "Пароль повинен містити не менше {2} і не більше {1} символів.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Новий пароль")]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Підтвердження нового паролю")]
    [Compare("NewPassword", ErrorMessage = "Паролі не співпадають.")]
    public string ConfirmPassword { get; set; }
}