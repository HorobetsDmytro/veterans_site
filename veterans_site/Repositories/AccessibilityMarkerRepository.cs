using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Repositories;

public class AccessibilityMarkerRepository : IAccessibilityMarkerRepository
{
    private readonly VeteranSupportDbContext _context;

    public AccessibilityMarkerRepository(VeteranSupportDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AccessibilityMarker>> GetAllMarkersAsync()
    {
        return await _context.AccessibilityMarkers
            .Include(m => m.User)
            .ToListAsync();
    }

    public async Task<AccessibilityMarker> GetMarkerByIdAsync(int id)
    {
        return await _context.AccessibilityMarkers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<AccessibilityMarker>> GetMarkersByUserIdAsync(string userId)
    {
        return await _context.AccessibilityMarkers
            .Where(m => m.UserId == userId)
            .Include(m => m.User)
            .ToListAsync();
    }

    public async Task AddMarkerAsync(AccessibilityMarker marker)
    {
        marker.CreatedAt = DateTime.Now;
        _context.AccessibilityMarkers.Add(marker);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateMarkerAsync(AccessibilityMarker marker)
    {
        marker.UpdatedAt = DateTime.Now;
        _context.AccessibilityMarkers.Update(marker);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteMarkerAsync(int id)
    {
        var marker = await _context.AccessibilityMarkers.FindAsync(id);
        if (marker != null)
        {
            _context.AccessibilityMarkers.Remove(marker);
            await _context.SaveChangesAsync();
        }
    }
}