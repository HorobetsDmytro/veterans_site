using veterans_site.Models;

namespace veterans_site.Interfaces;

public interface IResumeRepository : IGenericRepository<Resume>
{
    Task<IEnumerable<Resume>> GetResumesByUserIdAsync(string userId);
    Task<bool> HasLinkedApplicationsAsync(int resumeId);

}