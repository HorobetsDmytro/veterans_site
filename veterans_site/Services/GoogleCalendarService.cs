using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.DataProtection;
using veterans_site.Models;
using Event = Google.Apis.Calendar.v3.Data.Event;
using MyEvent = veterans_site.Models.Event;

namespace veterans_site.Services
{
    public class GoogleCalendarService
    {
        private readonly IConfiguration _configuration;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly string[] Scopes = { CalendarService.Scope.CalendarEvents };
        private readonly string ApplicationName;
        private readonly string ClientSecretsPath;

        public GoogleCalendarService(
            IConfiguration configuration,
            IDataProtectionProvider dataProtectionProvider)
        {
            _configuration = configuration;
            _dataProtectionProvider = dataProtectionProvider;
            ApplicationName = _configuration["GoogleCalendar:ApplicationName"];
            ClientSecretsPath = Path.Combine(Directory.GetCurrentDirectory(),
                _configuration["GoogleCalendar:ClientSecrets"]);
        }

        public async Task<string> GetAuthorizationUrl(string userId)
        {
            var clientSecrets = await GoogleClientSecrets.FromFileAsync(ClientSecretsPath);
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets.Secrets,
                Scopes = Scopes,
                DataStore = new FileDataStore("GoogleCalendarTokens")
            });

            var redirectUri = "https://localhost:7037/Events/GoogleCallback";

            return flow.CreateAuthorizationCodeRequest(redirectUri).Build().ToString();
        }

        public async Task<UserCredential> GetCredentialAsync(string userId, string code, string redirectUri)
        {
            try
            {
                var clientSecrets = await GoogleClientSecrets.FromFileAsync(ClientSecretsPath);
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets.Secrets,
                    Scopes = Scopes,
                    DataStore = new FileDataStore("GoogleCalendarTokens")
                });

                var token = await flow.ExchangeCodeForTokenAsync(
                    userId,
                    code,
                    redirectUri,
                    CancellationToken.None);

                return new UserCredential(flow, userId, token);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting credentials: {ex.Message}", ex);
            }
        }

        public async Task AddEventToCalendarAsync(MyEvent eventDetails, string userId, UserCredential credential)
        {
            try
            {
                var service = new CalendarService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

                var calendarEvent = new Event
                {
                    Summary = eventDetails.Title,
                    Location = eventDetails.Location,
                    Description = eventDetails.Description,
                    Start = new EventDateTime
                    {
                        DateTime = eventDetails.Date,
                        TimeZone = "Europe/Kiev",
                    },
                    End = new EventDateTime
                    {
                        DateTime = eventDetails.Date.AddMinutes(eventDetails.Duration),
                        TimeZone = "Europe/Kiev",
                    },
                    Reminders = new Google.Apis.Calendar.v3.Data.Event.RemindersData
                    {
                        UseDefault = false,
                        Overrides = new List<EventReminder>
                    {
                        new EventReminder
                        {
                            Method = "popup",
                            Minutes = (int)(eventDetails.Date - eventDetails.Date.Date).TotalMinutes
                        },
                        new EventReminder
                        {
                            Method = "popup",
                            Minutes = 60
                        }
                    }
                    }
                    };

                await service.Events.Insert(calendarEvent, "primary").ExecuteAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding event to calendar: {ex.Message}", ex);
            }
        }
    
        private string GetProtectedUserId(string userId)
        {
            var protector = _dataProtectionProvider.CreateProtector("GoogleCalendarUserId");
            return protector.Protect(userId);
        }

        private string GetUnprotectedUserId(string protectedUserId)
        {
            var protector = _dataProtectionProvider.CreateProtector("GoogleCalendarUserId");
            return protector.Unprotect(protectedUserId);
        }
    }
}
