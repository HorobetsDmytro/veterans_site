using veterans_site.Models;

namespace veterans_site.ViewModels;

public class AllJobApplicationsViewModel
{
    public IEnumerable<JobApplication> Applications { get; set; }
    public IEnumerable<Job> Jobs { get; set; }
    public string JobFilter { get; set; }
    public string StatusFilter { get; set; }
}