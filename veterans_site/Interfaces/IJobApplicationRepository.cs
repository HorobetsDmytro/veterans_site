using veterans_site.Models;

namespace veterans_site.Interfaces;

public interface IJobApplicationRepository : IGenericRepository<JobApplication>
{
    Task<IEnumerable<JobApplication>> GetApplicationsByUserIdAsync(string userId);
    Task<IEnumerable<JobApplication>> GetApplicationsByJobIdAsync(int jobId);
    Task<bool> HasUserAppliedAsync(string userId, int jobId);
    Task<IEnumerable<JobApplication>> GetAllApplicationsAsync();
    Task<int> GetApplicationsCountAsync(int jobId);

}