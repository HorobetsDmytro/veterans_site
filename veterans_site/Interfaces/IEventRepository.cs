using veterans_site.Models;

namespace veterans_site.Interfaces
{
    public interface IEventRepository : IGenericRepository<Event>
    {
        Task<IEnumerable<Event>> GetUpcomingEventsAsync();
        Task<IEnumerable<Event>> GetEventsByLocationAsync(string location);
        Task<IEnumerable<Event>> GetUserEventsAsync(string userId);
        Task<bool> IsUserRegisteredForEventAsync(string userId, int eventId);
        Task RegisterUserForEventAsync(string userId, int eventId);
        Task UnregisterUserFromEventAsync(string userId, int eventId);
        Task<int> GetEventParticipantsCountAsync(int eventId);
    }
}
