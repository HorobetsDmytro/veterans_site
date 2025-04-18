namespace veterans_site.Models;

public class SavedJob
{
    public int Id { get; set; }
    
    public int JobId { get; set; }
    public Job Job { get; set; }
    
    public string ApplicationUserId { get; set; }
    public ApplicationUser User { get; set; }
    
    public DateTime SavedDate { get; set; }
}