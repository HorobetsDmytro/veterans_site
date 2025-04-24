using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Repositories
{
    public class ResumeRepository : GenericRepository<Resume>, IResumeRepository
    {
        public ResumeRepository(VeteranSupportDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Resume>> GetResumesByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(r => r.ApplicationUserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }
        
        public async Task<bool> HasLinkedApplicationsAsync(int resumeId)
        {
            return await _context.JobApplications.AnyAsync(a => a.ResumeId == resumeId);
        }
    }
}