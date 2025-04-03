using veterans_site.Models;

namespace veterans_site.ViewModels;

public class ChatViewModel
{
    public ApplicationUser CurrentUser { get; set; }
    public ApplicationUser SelectedUser { get; set; }
    public List<ChatMessage> Messages { get; set; }
}