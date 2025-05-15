using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Repositories
{
    public class JobApplicationRepository : GenericRepository<JobApplication>, IJobApplicationRepository
    {
        public JobApplicationRepository(VeteranSupportDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<JobApplication>> GetApplicationsByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(a => a.Job)
                .Include(a => a.Resume)
                .Where(a => a.ApplicationUserId == userId)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobApplication>> GetApplicationsByJobIdAsync(int jobId)
        {
            return await _dbSet
                .Include(a => a.User)
                .Include(a => a.Resume)
                .Where(a => a.JobId == jobId)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();
        }

        public async Task<bool> HasUserAppliedAsync(string userId, int jobId)
        {
            return await _dbSet.AnyAsync(a => a.ApplicationUserId == userId && a.JobId == jobId);
        }

        public async Task<IEnumerable<JobApplication>> GetAllApplicationsAsync()
        {
            return await _context.JobApplications
                .Include(j => j.Job)
                .Include(j => j.User)
                .Include(j => j.Resume)
                .ToListAsync();
        }
        
        public async Task<int> GetApplicationsCountAsync(int jobId)
        {
            return await _context.JobApplications.CountAsync(a => a.JobId == jobId);
        }
        
        public async Task<List<int>> GetJobIdsWithStatusAsync(ApplicationStatus status)
        {
            return await _dbSet
                .Where(ja => ja.Status == status)
                .Select(ja => ja.JobId)
                .Distinct()
                .ToListAsync();
        }
    }
}