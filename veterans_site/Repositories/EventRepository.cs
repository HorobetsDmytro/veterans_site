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

        public async Task<int> GetUserEventsCount(string userId)
        {
            return await _context.EventParticipants
                .Where(ep => ep.UserId == userId)
                .CountAsync();
        }

        public async Task RemoveUserParticipationsAsync(string userId)
        {
            var participations = await _context.EventParticipants
                .Where(ep => ep.UserId == userId)
                .ToListAsync();

            if (participations.Any())
            {
                _context.EventParticipants.RemoveRange(participations);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Event>> GetUserEventsAsync(string userId, bool includeParticipants = false)
        {
            IQueryable<Event> query = _context.Events;

            if (includeParticipants)
            {
                query = query.Include(e => e.EventParticipants);
            }

            var events = await query
                .Where(e => e.EventParticipants.Any(ep => ep.UserId == userId))
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            return events;
        }

        public async Task<bool> IsUserRegisteredForEventAsync(int eventId, string userId)
        {
            return await _context.EventParticipants
                .AnyAsync(ep => ep.EventId == eventId && ep.UserId == userId);
        }

        public async Task CancelEventParticipationAsync(int eventId, string userId)
        {
            var participation = await _context.EventParticipants
                .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.UserId == userId);

            if (participation != null)
            {
                _context.EventParticipants.Remove(participation);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasActiveEventsAsync(string userId)
        {
            return await _context.Events
                .AnyAsync(e =>
                    e.EventParticipants.Any(ep => ep.UserId == userId) &&
                    e.Date > DateTime.Now &&
                    e.Status != EventStatus.Cancelled);
        }

        public async Task<Event> GetByIdWithParticipantsAsync(int id)
        {
            return await _context.Events
                .Include(e => e.EventParticipants)
                    .ThenInclude(ep => ep.User)
                .Include(e => e.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<ICollection<EventParticipant>> GetEventParticipantsAsync(int eventId)
        {
            return await _context.EventParticipants
                .Include(ep => ep.User)
                .Where(ep => ep.EventId == eventId)
                .ToListAsync();
        }
    }
}
