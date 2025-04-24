using System.ComponentModel.DataAnnotations;

namespace veterans_site.Models;

public class JobApplication
{
    public int Id { get; set; }
    
    public int JobId { get; set; }
    public Job Job { get; set; }
    
    public string ApplicationUserId { get; set; }
    public ApplicationUser User { get; set; }
    
    public int? ResumeId { get; set; }
    public Resume Resume { get; set; }
    
    [DataType(DataType.MultilineText)]
    public string CoverLetter { get; set; }
    
    public DateTime ApplicationDate { get; set; }
    
    public ApplicationStatus Status { get; set; }
    
    public string? StatusNote { get; set; }
}

public enum ApplicationStatus
{
    [Display(Name = "На розгляді")]
    Pending,
    
    [Display(Name = "Розглянуто")]
    Reviewed,
    
    [Display(Name = "Запрошено на співбесіду")]
    InterviewInvited,
    
    [Display(Name = "Відхилено")]
    Rejected,
    
    [Display(Name = "Прийнято")]
    Accepted
}