using veterans_site.Models;

namespace veterans_site.Interfaces;

public interface ISavedJobRepository : IGenericRepository<SavedJob>
{
    Task<IEnumerable<SavedJob>> GetSavedJobsByUserIdAsync(string userId);
    Task<bool> IsJobSavedByUserAsync(string userId, int jobId);
    Task<List<SavedJob>> GetSavedJobsByJobIdAsync(int jobId);
}