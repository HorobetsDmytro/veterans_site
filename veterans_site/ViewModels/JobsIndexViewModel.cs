using veterans_site.Models;

namespace veterans_site.ViewModels;

public class JobsIndexViewModel
{
    public IEnumerable<Job> Jobs { get; set; }
    public string Query { get; set; }
    public string Location { get; set; }
    public string Category { get; set; }
    public JobType? JobType { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public List<string> Categories { get; set; }
    public List<JobType> JobTypes { get; set; }
}