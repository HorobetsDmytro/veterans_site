using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using veterans_site.Data;

namespace veterans_site.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : Controller
    {
        private readonly VeteranSupportDbContext _context;

        public StatisticsController(VeteranSupportDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetDashboardData")]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var totalConsultations = await _context.Consultations.CountAsync();
                var totalEvents = await _context.Events.CountAsync();
                var totalNews = await _context.News.CountAsync();

                var consultationsByType = await _context.Consultations
                    .GroupBy(c => c.Type)
                    .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync();

                var consultationsByStatus = await _context.Consultations
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync();

                var sixMonthsAgo = DateTime.Now.AddMonths(-6);
                var currentDate = DateTime.Now;

                var monthlyConsultations = await _context.Consultations
                    .Where(c => c.DateTime >= sixMonthsAgo)
                    .GroupBy(c => new { Month = c.DateTime.Month, Year = c.DateTime.Year })
                    .Select(g => new { 
                        Month = g.Key.Month, 
                        Year = g.Key.Year, 
                        Count = g.Count() 
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                var allMonths = Enumerable.Range(0, 6)
                    .Select(i => new {
                        Year = currentDate.AddMonths(-i).Year,
                        Month = currentDate.AddMonths(-i).Month
                    })
                    .Reverse()
                    .ToList();

                var dbRegistrations = await _context.Users
                    .Where(u => u.RegistrationDate >= sixMonthsAgo)
                    .GroupBy(u => new { u.RegistrationDate.Year, u.RegistrationDate.Month })
                    .Select(g => new {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var monthlyRegistrations = allMonths
                    .GroupJoin(
                        dbRegistrations,
                        m => new { m.Year, m.Month },
                        r => new { r.Year, r.Month },
                        (m, rs) => new {
                            Year = m.Year,
                            Month = m.Month,
                            Count = rs.FirstOrDefault()?.Count ?? 0
                        })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();

                var monthLabels = monthlyRegistrations
                    .Select(x => new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy"))
                    .ToList();

                var registrationData = monthlyRegistrations.Select(x => x.Count).ToList();

                var eventsByCategory = await _context.Events
                    .GroupBy(e => e.Category)
                    .Select(g => new { Category = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync();

                var eventAttendance = await _context.Events
                    .Where(e => e.MaxParticipants > 0)
                    .Select(e => new {
                        EventId = e.Id,
                        Category = e.Category.ToString(),
                        MaxParticipants = e.MaxParticipants ?? 0,
                        ParticipantsCount = e.EventParticipants.Count
                    })
                    .ToListAsync();

                var groupedAttendance = eventAttendance
                    .GroupBy(x => x.Category)
                    .Select(g => new {
                        category = g.Key,
                        percentage = g.Average(x => 
                            x.MaxParticipants > 0 ? (double)x.ParticipantsCount / x.MaxParticipants * 100 : 0)
                    })
                    .ToList();

                return Ok(new {
                    totalUsers,
                    totalConsultations,
                    totalEvents,
                    totalNews,
                    
                    consultationTypes = consultationsByType.Select(x => new { label = x.Type, value = x.Count }),
                    
                    consultationStatuses = consultationsByStatus.Select(x => new { label = x.Status, value = x.Count }),
                    
                    consultationTrend = new {
                        labels = monthLabels,
                        data = monthlyConsultations
                            .GroupJoin(
                                allMonths,
                                c => new { c.Year, c.Month },
                                m => new { m.Year, m.Month },
                                (c, ms) => new { 
                                    Year = c?.Year ?? ms.First().Year, 
                                    Month = c?.Month ?? ms.First().Month, 
                                    Count = c?.Count ?? 0 
                                })
                            .OrderBy(x => x.Year)
                            .ThenBy(x => x.Month)
                            .Select(x => x.Count)
                    },
                    
                    eventCategories = eventsByCategory.Select(x => new { label = x.Category, value = x.Count }),
                    
                    eventAttendance = groupedAttendance,
                    
                    userRegistrations = new {
                        labels = monthLabels,
                        data = registrationData
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}