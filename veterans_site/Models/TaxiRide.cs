using System.ComponentModel.DataAnnotations;

namespace veterans_site.Models;

public class TaxiRide
{
    [Key]
    public int Id { get; set; }
    public string VeteranId { get; set; }
    public string DriverId { get; set; }
        
    [Required]
    public string StartAddress { get; set; }
        
    [Required]
    public string EndAddress { get; set; }
        
    [Required]
    public double StartLatitude { get; set; }
        
    [Required]
    public double StartLongitude { get; set; }
        
    [Required]
    public double EndLatitude { get; set; }
        
    [Required]
    public double EndLongitude { get; set; }

    public const double EstimatedPrice = 0;
    public int EstimatedDuration { get; set; }
    public double EstimatedDistance { get; set; }
    public const double ActualPrice = 0;
    public DateTime RequestTime { get; set; } = DateTime.Now;
    public DateTime? AcceptTime { get; set; }
    public DateTime? PickupTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public DateTime? ScheduledTime { get; set; }
    public TaxiRideStatus Status { get; set; } = TaxiRideStatus.Requested;
    public List<string>? CarTypes { get; set; }
    public virtual ApplicationUser Veteran { get; set; }
    public virtual ApplicationUser Driver { get; set; }
}
    
public enum TaxiRideStatus
{
    [Display(Name = "Запит надіслано")]  
    Requested,
    
    [Display(Name = "Прийнято")]  
    Accepted,
    
    [Display(Name = "Водій прибуває")]  
    DriverArriving,
    
    [Display(Name = "В дорозі")]  
    InProgress,
    
    [Display(Name = "Завершено")]  
    Completed,
    
    [Display(Name = "Скасовано")]  
    Canceled
}