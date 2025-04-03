using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using veterans_site.Data;
using veterans_site.Models;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace veterans_site.Controllers
{
    [Authorize(Roles = "Veteran")]
    public class ChatController : Controller
    {
        private readonly VeteranSupportDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        
        public ChatController(VeteranSupportDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }
        
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            
            var veterans = await _userManager.GetUsersInRoleAsync("Veteran");
            
            var filteredVeterans = veterans.Where(v => v.Id != currentUserId).ToList();
            
            return View(filteredVeterans);
        }
        
        public async Task<IActionResult> Conversation(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            if (string.IsNullOrEmpty(userId))
                return NotFound();
                
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();
                
            var messages = await _context.ChatMessages
                .Where(m => 
                    (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
                
            var unreadMessages = messages
                .Where(m => m.ReceiverId == currentUserId && !m.IsRead)
                .ToList();
                
            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }
                
                await _context.SaveChangesAsync();
            }
            
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.ReceiverId = userId;
            ViewBag.ReceiverName = $"{user.FirstName} {user.LastName}";
            ViewBag.ReceiverAvatar = user.AvatarPath;
            ViewBag.ReceiverIsOnline = user.IsOnline;
            ViewBag.ReceiverLastOnline = user.LastOnline;
            
            return View(messages);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetUnreadMessagesCount()
        {
            var currentUserId = _userManager.GetUserId(User);
            
            var count = await _context.ChatMessages
                .CountAsync(m => m.ReceiverId == currentUserId && !m.IsRead);
                
            return Json(new { count });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = _userManager.GetUserId(User);
            var count = await _context.ChatMessages
                .CountAsync(m => m.ReceiverId == currentUserId && !m.IsRead);
        
            return Json(new { count });
        }
        
        [HttpPost]
        public async Task<IActionResult> UploadFile()
        {
            var currentUserId = _userManager.GetUserId(User);
            var file = Request.Form.Files.FirstOrDefault();
            
            if (file == null)
                return BadRequest(new { error = "Файл не знайдено" });
                
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { error = "Файл занадто великий. Максимальний розмір 10MB." });
                
            var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedTypes.Contains(fileExtension))
                return BadRequest(new { error = "Непідтримуваний формат файлу" });
                
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "chat");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
                
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            var fileUrl = $"/uploads/chat/{uniqueFileName}";
            var fileType = GetFileType(fileExtension);
            
            return Ok(new { 
                fileUrl = fileUrl, 
                fileName = file.FileName,
                fileType = fileType,
            });
        }
        
        private string GetFileType(string extension)
        {
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                    return "image";
                case ".pdf":
                    return "pdf";
                case ".doc":
                case ".docx":
                    return "doc";
                case ".xls":
                case ".xlsx":
                    return "excel";
                default:
                    return "file";
            }
        }
    }
}