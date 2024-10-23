using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Repositories
{
    public class EventRepository : GenericRepository<Event>, IEventRepository
    {
        public EventRepository(VeteranSupportDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Event>> GetUpcomingEventsAsync()
        {
            return await FindAsync(e => e.Date > DateTime.Now);
        }

        public async Task<IEnumerable<Event>> GetEventsByLocationAsync(string location)
        {
            return await FindAsync(e => e.Location.Contains(location));
        }

        // Можна додати інші специфічні методи для роботи з подіями
        public async Task<IEnumerable<Event>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await FindAsync(e => e.Date >= startDate && e.Date <= endDate);
        }
    }
}
