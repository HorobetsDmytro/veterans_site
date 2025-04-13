using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.ViewModels;

namespace veterans_site.Controllers
{
    public class AccessibilityMapController : Controller
    {
        private readonly IAccessibilityMarkerRepository _markerRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccessibilityMapController(
            IAccessibilityMarkerRepository markerRepository, 
            UserManager<ApplicationUser> userManager)
        {
            _markerRepository = markerRepository;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMarkers()
        {
            var markers = await _markerRepository.GetAllMarkersAsync();
            
            var result = markers.Select(m => new
            {
                id = m.Id,
                latitude = m.Latitude,
                longitude = m.Longitude,
                title = m.Title,
                description = m.Description,
                hasRamp = m.HasRamp,
                hasBlindSupport = m.HasBlindSupport,
                hasElevator = m.HasElevator,
                hasAccessibleToilet = m.HasAccessibleToilet,
                hasParking = m.HasParking,
                address = m.Address,
                createdAt = m.CreatedAt,
                userName = m.User.UserName
            });
            
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMarkerDetails(int id, bool raw = false)
        {
            var marker = await _markerRepository.GetMarkerByIdAsync(id);
            if (marker == null)
            {
                return NotFound();
            }

            var viewModel = new MarkerDetailsViewModel
            {
                Id = marker.Id,
                Title = marker.Title,
                Description = marker.Description,
                Latitude = marker.Latitude,
                Longitude = marker.Longitude,
                HasRamp = marker.HasRamp,
                HasBlindSupport = marker.HasBlindSupport,
                HasElevator = marker.HasElevator,
                HasAccessibleToilet = marker.HasAccessibleToilet,
                HasParking = marker.HasParking,
                Address = marker.Address,
                CreatedAt = marker.CreatedAt,
                UserName = string.Concat(marker.User.LastName, " ", marker.User.FirstName)
            };

            if (raw)
            {
                return Json(viewModel);
            }

            return PartialView("_MarkerDetails", viewModel);
        }
        
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMarkersByUser(string userId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
    
            if (currentUser == null)
            {
                return Unauthorized();
            }
    
            string targetUserId = userId ?? currentUser.Id;
    
            if (targetUserId != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var markers = await _markerRepository.GetMarkersByUserIdAsync(targetUserId);
    
            var result = markers.Select(m => new
            {
                id = m.Id,
                latitude = double.Parse(m.Latitude, CultureInfo.InvariantCulture),
                longitude = double.Parse(m.Longitude, CultureInfo.InvariantCulture),
                title = m.Title,
                description = m.Description,
                hasRamp = m.HasRamp,
                hasBlindSupport = m.HasBlindSupport,
                hasElevator = m.HasElevator,
                hasAccessibleToilet = m.HasAccessibleToilet,
                hasParking = m.HasParking,
                address = m.Address,
                createdAt = m.CreatedAt
            });
    
            return Json(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddMarker(AddMarkerViewModel model)
        {
            Console.WriteLine($"Received data - Title: '{model.Title}', Lat: {model.Latitude}, Lng: {model.Longitude}");
            
            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    Console.WriteLine($"Field: {entry.Key}, Valid: {entry.Value.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid}");
                    foreach (var error in entry.Value.Errors)
                    {
                        Console.WriteLine($"-- Error: {error.ErrorMessage}");
                    }
                }
                
                var errors = ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .Select(e => new {
                        Field = e.Key,
                        Errors = e.Value.Errors.Select(er => er.ErrorMessage).ToList()
                    }).ToList();
                
                return Json(new { success = false, errors = errors });
            }
            
            var user = await _userManager.GetUserAsync(User);
            
            if (!double.TryParse(model.Latitude, System.Globalization.NumberStyles.Any, 
                                System.Globalization.CultureInfo.InvariantCulture, out double latitude) ||
                !double.TryParse(model.Longitude, System.Globalization.NumberStyles.Any, 
                                System.Globalization.CultureInfo.InvariantCulture, out double longitude))
            {
                return Json(new { success = false, errors = new[] { 
                    new { Field = "Latitude/Longitude", Errors = new[] { "Неправильний формат координат" } } 
                }});
            }
            
            if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            {
                return Json(new { success = false, errors = new[] { 
                    new { Field = "Latitude/Longitude", Errors = new[] { "Координати виходять за допустимі межі" } } 
                }});
            }
            
            var marker = new AccessibilityMarker
            {
                UserId = user.Id,
                Title = model.Title,
                Description = model.Description,
                Latitude = latitude.ToString(CultureInfo.InvariantCulture),
                Longitude = longitude.ToString(CultureInfo.InvariantCulture),
                HasRamp = model.HasRamp,
                HasBlindSupport = model.HasBlindSupport,
                HasElevator = model.HasElevator,
                HasAccessibleToilet = model.HasAccessibleToilet,
                HasParking = model.HasParking,
                Address = model.Address
            };
            
            await _markerRepository.AddMarkerAsync(marker);
            
            return Json(new { success = true, id = marker.Id });
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateMarker(int id, UpdateMarkerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var marker = await _markerRepository.GetMarkerByIdAsync(id);
                if (marker == null)
                {
                    return NotFound();
                }

                var user = await _userManager.GetUserAsync(User);
                if (marker.UserId != user.Id && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                marker.Title = model.Title;
                marker.Description = model.Description;
                marker.HasRamp = model.HasRamp;
                marker.HasBlindSupport = model.HasBlindSupport;
                marker.HasElevator = model.HasElevator;
                marker.HasAccessibleToilet = model.HasAccessibleToilet;
                marker.HasParking = model.HasParking;
                marker.Address = model.Address;

                await _markerRepository.UpdateMarkerAsync(marker);
                
                return Json(new { success = true });
            }
            
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors) });
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteMarker(int id)
        {
            var marker = await _markerRepository.GetMarkerByIdAsync(id);
            if (marker == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (marker.UserId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            await _markerRepository.DeleteMarkerAsync(id);
            
            return Json(new { success = true });
        }
    }
}