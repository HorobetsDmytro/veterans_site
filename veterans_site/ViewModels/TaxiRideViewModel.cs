namespace veterans_site.ViewModels;

public class TaxiRideViewModel
{
    public string StartAddress { get; set; }
    public string EndAddress { get; set; }
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public double EndLatitude { get; set; }
    public double EndLongitude { get; set; }
    public double EstimatedDistance
    {
        get => _estimatedDistance;
        set => _estimatedDistance = Math.Round(value, 2);
    }
    private double _estimatedDistance;

    public int EstimatedDuration { get; set; }
    public DateTime? ScheduledTime { get; set; }
}