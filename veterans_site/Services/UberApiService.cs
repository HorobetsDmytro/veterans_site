using System.Net.Http.Headers;

namespace veterans_site.Services;

public class UberApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    
    public UberApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;

        var apiBaseUrl = _configuration["UberAPI:ApiBaseUrl"];
        _httpClient.BaseAddress = new Uri(apiBaseUrl);
            
        var clientId = _configuration["UberAPI:ClientId"];
        var clientSecret = _configuration["UberAPI:ClientSecret"];
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", $"{clientId}:{clientSecret}");
    }
    
    public async Task<EstimateResponse> GetPriceEstimateAsync(
        double startLat, double startLng, 
        double endLat, double endLng,
        double? distanceKm = null, 
        int? durationMinutes = null)
    {
        if (distanceKm.HasValue && durationMinutes.HasValue)
        {
            var price = 0;
        
            return new EstimateResponse
            {
                EstimatedPrice = price,
                EstimatedDuration = durationMinutes.Value * 60 * 5,
                Distance = distanceKm.Value
            };
        }
    
        var distance = CalculateDistance(startLat, startLng, endLat, endLng);
        var duration = distance * 5 * 60;
        var estimatedPrice = 0;
    
        return new EstimateResponse
        {
            EstimatedPrice = estimatedPrice,
            EstimatedDuration = duration,
            Distance = distance
        };
    }
    
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadius = 6371;
        
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = EarthRadius * c;
        
        return Math.Round(distance, 2);
    }
    
    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}

public class EstimateResponse
{
    public double EstimatedPrice { get; set; }
    public double EstimatedDuration { get; set; }
    public double Distance { get; set; }
}