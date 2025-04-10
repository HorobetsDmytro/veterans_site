namespace veterans_site.Models;

public class UserLastReadGeneralChat
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int LastReadMessageId { get; set; }
    public DateTime LastReadAt { get; set; }
}