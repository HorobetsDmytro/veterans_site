using System.ComponentModel.DataAnnotations;
using veterans_site.Models;

namespace veterans_site.ViewModels;

public class JobApplicationViewModel
{
    public int JobId { get; set; }
    public Job Job { get; set; }
    
    [Display(Name = "Резюме")]
    public int? ResumeId { get; set; }
    
    [Display(Name = "Супровідний лист")]
    [DataType(DataType.MultilineText)]
    public string CoverLetter { get; set; }
    
    public List<Resume> AvailableResumes { get; set; } = new List<Resume>();
}