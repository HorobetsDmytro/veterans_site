namespace veterans_site.ViewModels;

public class EstimateRequestViewModel
{
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public double EndLatitude { get; set; }
    public double EndLongitude { get; set; }
    public double? DistanceKm { get; set; }
    public int? DurationMinutes { get; set; }
}