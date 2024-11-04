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

        Task<int> GetUserEventsCount(string userId);
        Task RemoveUserParticipationsAsync(string userId);
        Task<IEnumerable<Event>> GetUserEventsAsync(string userId, bool includeParticipants = false);
        Task<bool> IsUserRegisteredForEventAsync(int eventId, string userId);
        Task CancelEventParticipationAsync(int eventId, string userId);
        Task<bool> HasActiveEventsAsync(string userId);
        Task<Event> GetByIdWithParticipantsAsync(int eventId);
        Task<ICollection<EventParticipant>> GetEventParticipantsAsync(int eventId);
    }
}
