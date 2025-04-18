using System.ComponentModel.DataAnnotations;

namespace veterans_site.Models;

public class Resume
{
    public int Id { get; set; }
    
    [Required]
    public string ApplicationUserId { get; set; }
    public ApplicationUser User { get; set; }
    
    [Required]
    public string FullName { get; set; }
    
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }
    
    [DataType(DataType.PhoneNumber)]
    public string Phone { get; set; }
    
    public string Skills { get; set; }
    
    [DataType(DataType.MultilineText)]
    public string Experience { get; set; }
    
    [DataType(DataType.MultilineText)]
    public string Education { get; set; }
    
    [DataType(DataType.MultilineText)]
    public string AdditionalInfo { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime? LastUpdated { get; set; }
    
    public string FilePath { get; set; }
    
    public bool IsPublic { get; set; } = false;
}