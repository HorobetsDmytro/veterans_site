using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Repositories
{
    public class SavedJobRepository : GenericRepository<SavedJob>, ISavedJobRepository
    {
        public SavedJobRepository(VeteranSupportDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SavedJob>> GetSavedJobsByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(sj => sj.Job)
                .Where(sj => sj.ApplicationUserId == userId)
                .OrderByDescending(sj => sj.SavedDate)
                .ToListAsync();
        }

        public async Task<bool> IsJobSavedByUserAsync(string userId, int jobId)
        {
            return await _dbSet.AnyAsync(sj => sj.ApplicationUserId == userId && sj.JobId == jobId);
        }
        
        public async Task<List<SavedJob>> GetSavedJobsByJobIdAsync(int jobId)
        {
            return await _context.SavedJobs
                .Where(sj => sj.JobId == jobId)
                .ToListAsync();
        }
    }
}