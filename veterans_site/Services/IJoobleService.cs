using veterans_site.Models;

namespace veterans_site.Services;

public interface IJoobleService
{
    Task<List<Job>> SearchJobsAsync(string keywords, string location, int count = 20);
    Task SyncJobsWithDatabaseAsync();
}