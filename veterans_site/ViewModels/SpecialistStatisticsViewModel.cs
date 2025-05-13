using veterans_site.Models;

namespace veterans_site.ViewModels;

public class SpecialistStatisticsViewModel
{
    public int TotalConsultations { get; set; }
    public int UpcomingConsultations { get; set; }
    public int CompletedConsultations { get; set; }
    public int CancelledConsultations { get; set; }
    public string PeriodLabel { get; set; } = "Весь час";
    
    public int? PreviousPeriodConsultations { get; set; }
    public int? PreviousPeriodCompleted { get; set; }
    
    public Dictionary<ConsultationType, int> ConsultationsByType { get; set; } = new();
    public Dictionary<ConsultationFormat, int> ConsultationsByFormat { get; set; } = new();
    
    public Dictionary<string, MonthlyStats> MonthlyTrend { get; set; } = new();
    
    public double AverageGroupAttendance { get; set; }
    public Dictionary<ConsultationType, double> AttendanceByType { get; set; } = new();
    
    public double TotalEarnings { get; set; }
    public Dictionary<string, double> MonthlyEarnings { get; set; } = new();
    public double AverageEarningsPerConsultation { get; set; }
    
    public Dictionary<string, int> DailyConsultationCounts { get; set; } = new();
    
    public int UniqueVeteransServed { get; set; }
    public double RepeatVisitRate { get; set; }
    
    public Dictionary<int, int> PopularTimeSlots { get; set; } = new();
    
    public double GetCompletionRate()
    {
        if (TotalConsultations == 0) return 0;
        return (double)CompletedConsultations / TotalConsultations * 100;
    }
    
    public double GetCancellationRate()
    {
        if (TotalConsultations == 0) return 0;
        return (double)CancelledConsultations / TotalConsultations * 100;
    }
    
    public double? GetConsultationGrowthRate()
    {
        if (PreviousPeriodConsultations.HasValue && PreviousPeriodConsultations > 0)
        {
            return ((double)TotalConsultations / PreviousPeriodConsultations.Value - 1) * 100;
        }
        return null;
    }
}

public class MonthlyStats
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
}