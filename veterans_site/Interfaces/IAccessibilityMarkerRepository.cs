using veterans_site.Models;

namespace veterans_site.Interfaces;

public interface IAccessibilityMarkerRepository
{
    Task<IEnumerable<AccessibilityMarker>> GetAllMarkersAsync();
    Task<AccessibilityMarker> GetMarkerByIdAsync(int id);
    Task<IEnumerable<AccessibilityMarker>> GetMarkersByUserIdAsync(string userId);
    Task AddMarkerAsync(AccessibilityMarker marker);
    Task UpdateMarkerAsync(AccessibilityMarker marker);
    Task DeleteMarkerAsync(int id);
}