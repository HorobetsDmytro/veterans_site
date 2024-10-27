using Microsoft.EntityFrameworkCore;
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

        public async Task<IEnumerable<Event>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await FindAsync(e => e.Date >= startDate && e.Date <= endDate);
        }

        public async Task<IEnumerable<Event>> GetUserEventsAsync(string userId)
        {
            return await _context.Events
                .Include(e => e.EventParticipants)
                .Where(e => e.EventParticipants.Any(ep => ep.UserId == userId))
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task<bool> IsUserRegisteredForEventAsync(string userId, int eventId)
        {
            return await _context.EventParticipants
                .AnyAsync(ep => ep.EventId == eventId && ep.UserId == userId);
        }

        public async Task RegisterUserForEventAsync(string userId, int eventId)
        {
            var eventParticipant = new EventParticipant
            {
                EventId = eventId,
                UserId = userId,
                RegistrationDate = DateTime.Now
            };

            await _context.EventParticipants.AddAsync(eventParticipant);
            await _context.SaveChangesAsync();
        }

        public async Task UnregisterUserFromEventAsync(string userId, int eventId)
        {
            var registration = await _context.EventParticipants
                .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.UserId == userId);

            if (registration != null)
            {
                _context.EventParticipants.Remove(registration);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetEventParticipantsCountAsync(int eventId)
        {
            return await _context.EventParticipants
                .CountAsync(ep => ep.EventId == eventId);
        }
    }
}
