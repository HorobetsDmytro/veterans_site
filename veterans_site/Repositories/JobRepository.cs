using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Repositories
{
    public class JobRepository : GenericRepository<Job>, IJobRepository
    {
        public JobRepository(VeteranSupportDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Job>> GetActiveJobsAsync(bool includeExternal = true)
        {
            var query = _dbSet.Where(j => j.ExpiryDate == null || j.ExpiryDate > DateTime.Now);
            
            if (!includeExternal)
                query = query.Where(j => !j.IsExternal);
                
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Job>> SearchJobsAsync(string query, string location, string category, JobType? jobType)
        {
            var jobs = _dbSet.AsQueryable();
            
            if (!string.IsNullOrEmpty(query))
            {
                query = query.ToLower();
                jobs = jobs.Where(j => j.Title.ToLower().Contains(query) || 
                                      j.Description.ToLower().Contains(query) || 
                                      j.Company.ToLower().Contains(query));
            }
            
            if (!string.IsNullOrEmpty(location))
            {
                location = location.ToLower();
                jobs = jobs.Where(j => j.Location.ToLower().Contains(location));
            }
            
            if (!string.IsNullOrEmpty(category))
            {
                jobs = jobs.Where(j => j.Category == category);
            }
            
            if (jobType.HasValue)
            {
                jobs = jobs.Where(j => j.JobType == jobType.Value);
            }
            
            return await jobs.ToListAsync();
        }

        public async Task<Job> GetByExternalIdAsync(string externalId)
        {
            return await _dbSet.FirstOrDefaultAsync(j => j.ExternalId == externalId);
        }

        public DbContext GetDbContext()
        {
            return _context;
        }
    }
}