using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace veterans_site.Models;

public class ChatMessage
{
    public int Id { get; set; }
        
    public string SenderId { get; set; }
    [ForeignKey("SenderId")]
    public ApplicationUser Sender { get; set; }
        
    public string ReceiverId { get; set; }
    [ForeignKey("ReceiverId")]
    public ApplicationUser Receiver { get; set; }
        
    public string Content { get; set; }
        
    public DateTime SentAt { get; set; } = DateTime.Now;
        
    public bool IsRead { get; set; } = false;
    
    public bool IsEdited { get; set; } = false;
    
    public DateTime? EditedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public string? FileName { get; set; }
    
    public string? FilePath { get; set; }
    
    public string? FileType { get; set; }
    
    public bool HasFile => !string.IsNullOrEmpty(FilePath);
}