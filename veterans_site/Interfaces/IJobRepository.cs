using Microsoft.EntityFrameworkCore;
using veterans_site.Models;

namespace veterans_site.Interfaces;

public interface IJobRepository : IGenericRepository<Job>
{
    Task<IEnumerable<Job>> GetActiveJobsAsync(bool includeExternal = true);
    Task<IEnumerable<Job>> SearchJobsAsync(string query, string location, string category, JobType? jobType);
    Task<Job> GetByExternalIdAsync(string externalId);
    DbContext GetDbContext();
}