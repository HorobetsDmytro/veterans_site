namespace veterans_site.Models
{
    public class RoleChangeRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string NewRole { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryTime { get; set; }
        public bool IsConfirmed { get; set; }
    }
}
