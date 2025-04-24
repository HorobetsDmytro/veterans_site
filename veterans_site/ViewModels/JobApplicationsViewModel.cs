using veterans_site.Models;

namespace veterans_site.ViewModels;

public class JobApplicationsViewModel
{
    public Job Job { get; set; }
    public List<JobApplication> Applications { get; set; } = new List<JobApplication>();
}