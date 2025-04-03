namespace veterans_site.Models;

public class UserConnection
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string ConnectionId { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
}