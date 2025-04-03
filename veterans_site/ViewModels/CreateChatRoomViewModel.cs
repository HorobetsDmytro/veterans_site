using System.ComponentModel.DataAnnotations;

namespace veterans_site.ViewModels;

public class CreateChatRoomViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    public string Description { get; set; }

    public bool IsPrivate { get; set; } = false;
}