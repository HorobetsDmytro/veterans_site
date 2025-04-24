using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace veterans_site.Models;

public class Job
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; }
    
    [Required]
    public string Company { get; set; }
    
    public string Location { get; set; }
    
    [DataType(DataType.MultilineText)]
    public string Description { get; set; }
    
    public string? Requirements { get; set; }
    
    [DataType(DataType.Currency)]
    public decimal? Salary { get; set; }
    
    public string? ContactEmail { get; set; }
    
    public string? ContactPhone { get; set; }
    
    public DateTime PostedDate { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    public JobType JobType { get; set; }
    
    public string Category { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsExternal { get; set; }
    
    public string? ExternalId { get; set; }
    
    public string? ExternalUrl { get; set; }
    
    [NotMapped]
    public bool IsSaved { get; set; }
    
    [NotMapped]
    public bool IsApplied { get; set; }
    
    public int ApplicationsCount { get; set; }
    
    public ICollection<JobApplication> Applications { get; set; }
    public ICollection<SavedJob> SavedJobs { get; set; }
}

public enum JobType
{
    [Display(Name = "Повна зайнятість")]
    FullTime,
    
    [Display(Name = "Часткова зайнятість")]
    PartTime,
    
    [Display(Name = "Контракт")]
    Contract,
    
    [Display(Name = "Фріланс")]
    Freelance,
    
    [Display(Name = "Стажування")]
    Internship
}