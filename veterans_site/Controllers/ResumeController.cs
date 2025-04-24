using System.Text;
using DinkToPdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.ViewModels;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace veterans_site.Controllers;

[Authorize]
public class ResumeController : Controller
{
    private readonly IResumeRepository _resumeRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    
    public ResumeController(
        IResumeRepository resumeRepository,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _resumeRepository = resumeRepository;
        _userManager = userManager;
        _environment = environment;
    }
    
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var resumes = await _resumeRepository.GetResumesByUserIdAsync(userId);
        
        return View(resumes);
    }
    
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ResumeViewModel viewModel)
    {
        if (viewModel.ResumeInputType == "file")
        {
            if (viewModel.ResumeFile == null)
            {
                ModelState.AddModelError("ResumeFile", "Файл резюме обов'язковий при виборі цього способу");
                return View(viewModel);
            }
            
            foreach (var key in ModelState.Keys.Where(k => k != "ResumeFile" && k != "ResumeInputType").ToList())
            {
                ModelState.Remove(key);
            }
        }
        else if (viewModel.ResumeInputType == "manual")
        {
            ModelState.Remove("ResumeFile");
        }
        
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            
            var resume = new Resume
            {
                ApplicationUserId = user.Id,
                FullName = viewModel.FullName ?? $"{user.FirstName} {user.LastName}",
                Email = viewModel.Email ?? user.Email,
                Phone = viewModel.Phone,
                Skills = viewModel.Skills,
                Experience = viewModel.Experience,
                Education = viewModel.Education,
                AdditionalInfo = viewModel.AdditionalInfo,
                CreatedDate = DateTime.Now,
                IsPublic = true
            };
            
            if (viewModel.ResumeFile != null)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                
                var uniqueFileName = $"{Guid.NewGuid()}_{viewModel.ResumeFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.ResumeFile.CopyToAsync(fileStream);
                }
                
                resume.FilePath = $"/uploads/resumes/{uniqueFileName}";
            }
            
            await _resumeRepository.AddAsync(resume);
            
            return RedirectToAction(nameof(Index));
        }
        
        return View(viewModel);
    }
    
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var resume = await _resumeRepository.GetByIdAsync(id);
        
        if (resume == null)
            return NotFound();
        
        var userId = _userManager.GetUserId(User);
        
        if (resume.ApplicationUserId != userId && !User.IsInRole("Admin"))
            return Forbid();
        
        string resumeInputType = string.IsNullOrEmpty(resume.FilePath) ? "manual" : "file";
        
        var viewModel = new ResumeViewModel
        {
            Id = resume.Id,
            FullName = resume.FullName,
            Email = resume.Email,
            Phone = resume.Phone,
            Skills = resume.Skills,
            Experience = resume.Experience,
            Education = resume.Education,
            AdditionalInfo = resume.AdditionalInfo,
            ExistingFilePath = resume.FilePath,
            ResumeInputType = resumeInputType,
            OriginalResumeInputType = resumeInputType
        };
        
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ResumeViewModel viewModel)
    {
        if (id != viewModel.Id)
            return NotFound();
        
        var resume = await _resumeRepository.GetByIdAsync(id);
        
        if (resume == null)
            return NotFound();
        
        var userId = _userManager.GetUserId(User);
        
        if (resume.ApplicationUserId != userId && !User.IsInRole("Admin"))
            return Forbid();
        
        bool typeChanged = viewModel.OriginalResumeInputType != viewModel.ResumeInputType;
        
        if (typeChanged && viewModel.ResumeInputType == "manual")
        {
            if (string.IsNullOrEmpty(viewModel.FullName) || string.IsNullOrEmpty(viewModel.Email))
            {
                ModelState.AddModelError("", "При зміні типу резюме на текстовий потрібно заповнити обов'язкові поля");
                return View(viewModel);
            }
        }
        
        if (typeChanged && viewModel.ResumeInputType == "file" && viewModel.ResumeFile == null && string.IsNullOrEmpty(viewModel.ExistingFilePath))
        {
            ModelState.AddModelError("ResumeFile", "При зміні типу резюме на файловий необхідно завантажити файл");
            return View(viewModel);
        }
        
        if (ModelState.IsValid)
        {
            resume.FullName = viewModel.FullName;
            resume.Email = viewModel.Email;
            resume.Phone = viewModel.Phone;
            
            if (viewModel.ResumeInputType == "file")
            {
                if (viewModel.ResumeFile != null)
                {
                    if (!string.IsNullOrEmpty(resume.FilePath))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, resume.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                    
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                    
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);
                    
                    var uniqueFileName = $"{Guid.NewGuid()}_{viewModel.ResumeFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.ResumeFile.CopyToAsync(fileStream);
                    }
                    
                    resume.FilePath = $"/uploads/resumes/{uniqueFileName}";
                }
                
                if (typeChanged || viewModel.OriginalResumeInputType == "file")
                {
                    resume.Skills = null;
                    resume.Experience = null;
                    resume.Education = null;
                    resume.AdditionalInfo = null;
                }
            }
            else
            {
                resume.Skills = viewModel.Skills;
                resume.Experience = viewModel.Experience;
                resume.Education = viewModel.Education;
                resume.AdditionalInfo = viewModel.AdditionalInfo;
                
                if (typeChanged && !string.IsNullOrEmpty(resume.FilePath))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath, resume.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                    resume.FilePath = null;
                }
            }
            
            resume.IsPublic = true;
            resume.LastUpdated = DateTime.Now;
            
            await _resumeRepository.UpdateAsync(resume);
            
            return RedirectToAction(nameof(Index));
        }
        
        return View(viewModel);
    }
    
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var resume = await _resumeRepository.GetByIdAsync(id);
        
        if (resume == null)
            return NotFound();
        
        var userId = _userManager.GetUserId(User);
        
        if (resume.ApplicationUserId != userId && !User.IsInRole("Admin"))
            return Forbid();
        
        bool hasApplications = await _resumeRepository.HasLinkedApplicationsAsync(id);
        
        if (hasApplications)
        {
            TempData["HasApplications"] = true;
        }
        
        return View(resume);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var resume = await _resumeRepository.GetByIdAsync(id);
        
        if (resume == null)
            return NotFound();
        
        var userId = _userManager.GetUserId(User);
        
        if (resume.ApplicationUserId != userId && !User.IsInRole("Admin"))
            return Forbid();
        
        bool hasApplications = await _resumeRepository.HasLinkedApplicationsAsync(id);
        
        if (hasApplications)
        {
            TempData["Error"] = "Неможливо видалити резюме, оскільки воно використовується в заявках на вакансії.";
            return RedirectToAction(nameof(Delete), new { id });
        }
        
        if (!string.IsNullOrEmpty(resume.FilePath))
        {
            var filePath = Path.Combine(_environment.WebRootPath, resume.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        
        await _resumeRepository.DeleteAsync(id);
        
        return RedirectToAction(nameof(Index));
    }
    
    public async Task<IActionResult> Download(int id)
    {
        var resume = await _resumeRepository.GetByIdAsync(id);
        
        if (resume == null)
            return NotFound();
        
        var userId = _userManager.GetUserId(User);
        
        if (resume.ApplicationUserId != userId && !User.IsInRole("Admin") && !resume.IsPublic)
            return Forbid();
        
        if (!string.IsNullOrEmpty(resume.FilePath))
        {
            var filePath = Path.Combine(_environment.WebRootPath, resume.FilePath.TrimStart('/'));
            
            if (!System.IO.File.Exists(filePath))
                return NotFound();
            
            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            
            memory.Position = 0;
            
            var fileName = Path.GetFileName(filePath);
            return File(memory, "application/pdf", fileName);
        }
        
        try
        {
            var user = await _userManager.FindByIdAsync(resume.ApplicationUserId);
            string avatarPath = string.IsNullOrEmpty(user.AvatarPath) 
                ? "/uploads/avatars/default-avatar.png" 
                : user.AvatarPath;
                
            string avatarFullPath = Path.Combine(_environment.WebRootPath, avatarPath.TrimStart('/'));
        
            string avatarBase64 = "";
            if (System.IO.File.Exists(avatarFullPath))
            {
                byte[] avatarBytes = System.IO.File.ReadAllBytes(avatarFullPath);
                avatarBase64 = Convert.ToBase64String(avatarBytes);
                string extension = Path.GetExtension(avatarFullPath).TrimStart('.').ToLower();
                if (extension == "jpg" || extension == "jpeg")
                    extension = "jpeg";
                avatarBase64 = $"data:image/{extension};base64,{avatarBase64}";
            }
            
            string html = GenerateResumeHtml(resume, avatarBase64);
        
            try
            {
                var pdfBytes = await GeneratePdfWithPuppeteer(html);
        
                return File(pdfBytes, "application/pdf", $"Resume_{resume.FullName.Replace(" ", "_")}.pdf");
            }
            catch (Exception pdfEx)
            {
                var htmlBytes = Encoding.UTF8.GetBytes(html);
                return File(htmlBytes, "text/html", $"Resume_{resume.FullName.Replace(" ", "_")}.html");
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Помилка при створенні резюме: {ex.Message}";
            Console.WriteLine($"Error: {ex.Message}");
        
            var textContent = GenerateResumeText(resume);
            var textBytes = Encoding.UTF8.GetBytes(textContent);
        
            return File(textBytes, "text/plain", $"Resume_{resume.FullName.Replace(" ", "_")}.txt");
        }
    }
    
    private string GenerateResumeText(Resume resume)
    {
        var text = new StringBuilder();
    
        text.AppendLine($"РЕЗЮМЕ: {resume.FullName}");
        text.AppendLine($"Email: {resume.Email}");
    
        if (!string.IsNullOrEmpty(resume.Phone))
            text.AppendLine($"Телефон: {resume.Phone}");
    
        text.AppendLine();
    
        if (!string.IsNullOrEmpty(resume.Skills))
        {
            text.AppendLine("НАВИЧКИ:");
            text.AppendLine(resume.Skills);
            text.AppendLine();
        }
    
        if (!string.IsNullOrEmpty(resume.Experience))
        {
            text.AppendLine("ДОСВІД РОБОТИ:");
            text.AppendLine(resume.Experience);
            text.AppendLine();
        }
    
        if (!string.IsNullOrEmpty(resume.Education))
        {
            text.AppendLine("ОСВІТА:");
            text.AppendLine(resume.Education);
            text.AppendLine();
        }
    
        if (!string.IsNullOrEmpty(resume.AdditionalInfo))
        {
            text.AppendLine("ДОДАТКОВА ІНФОРМАЦІЯ:");
            text.AppendLine(resume.AdditionalInfo);
            text.AppendLine();
        }
    
        text.AppendLine($"Резюме створено: {resume.CreatedDate:dd.MM.yyyy}");
        if (resume.LastUpdated.HasValue)
            text.AppendLine($"Оновлено: {resume.LastUpdated.Value:dd.MM.yyyy}");
    
        return text.ToString();
    }

    private string GenerateResumeHtml(Resume resume, string avatarBase64)
    {
        var html = new StringBuilder();
        
        html.Append(@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <title>Резюме</title>
            <style>
                body {
                    font-family: 'Arial', sans-serif;
                    color: #333;
                    line-height: 1.6;
                    margin: 0;
                    padding: 0;
                    background-color: #f9f9f9;
                }
                .container {
                    max-width: 800px;
                    margin: 0 auto;
                    padding: 20px;
                    background-color: #fff;
                    box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
                }
                .header {
                    display: flex;
                    align-items: center;
                    padding-bottom: 20px;
                    border-bottom: 2px solid #0072CE;
                    margin-bottom: 30px;
                }
                .avatar {
                    width: 120px;
                    height: 120px;
                    border-radius: 50%;
                    object-fit: cover;
                    margin-right: 30px;
                    border: 3px solid #0072CE;
                }
                .header-text {
                    flex: 1;
                }
                h1 {
                    color: #0072CE;
                    margin: 0 0 10px 0;
                    font-size: 28px;
                }
                h2 {
                    color: #0072CE;
                    font-size: 20px;
                    margin: 20px 0 10px 0;
                    border-bottom: 1px solid #ddd;
                    padding-bottom: 5px;
                }
                .info-item {
                    margin-bottom: 8px;
                }
                .info-label {
                    font-weight: bold;
                    margin-right: 10px;
                    color: #555;
                }
                .skills-list {
                    display: flex;
                    flex-wrap: wrap;
                    margin-top: 10px;
                }
                .skill-item {
                    background-color: #e9f5ff;
                    color: #0072CE;
                    padding: 5px 15px;
                    margin: 0 10px 10px 0;
                    border-radius: 50px;
                    font-size: 14px;
                }
                .section {
                    margin-bottom: 25px;
                }
                .section-content {
                    white-space: pre-line;
                    margin-top: 10px;
                }
                .footer {
                    text-align: center;
                    margin-top: 30px;
                    font-size: 12px;
                    color: #888;
                    border-top: 1px solid #ddd;
                    padding-top: 15px;
                }
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
        ");
        
        if (!string.IsNullOrEmpty(avatarBase64))
        {
            html.Append($@"<img src='{avatarBase64}' class='avatar' alt='User Avatar'>");
        }
        
        html.Append($@"
                    <div class='header-text'>
                        <h1>{resume.FullName}</h1>
                        <div class='info-item'><span class='info-label'>Email:</span> {resume.Email}</div>
        ");
        
        if (!string.IsNullOrEmpty(resume.Phone))
        {
            html.Append($@"<div class='info-item'><span class='info-label'>Телефон:</span> {resume.Phone}</div>");
        }
        
        html.Append(@"
                    </div>
                </div>
        ");
        
        if (!string.IsNullOrEmpty(resume.Skills))
        {
            html.Append(@"
                <div class='section'>
                    <h2>Навички</h2>
                    <div class='skills-list'>
            ");
            
            var skills = resume.Skills.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var skill in skills)
            {
                if (!string.IsNullOrWhiteSpace(skill))
                {
                    html.Append($@"<div class='skill-item'>{skill.Trim()}</div>");
                }
            }
            
            html.Append(@"
                    </div>
                </div>
            ");
        }
        
        if (!string.IsNullOrEmpty(resume.Experience))
        {
            html.Append($@"
                <div class='section'>
                    <h2>Досвід роботи</h2>
                    <div class='section-content'>{resume.Experience}</div>
                </div>
            ");
        }
        
        if (!string.IsNullOrEmpty(resume.Education))
        {
            html.Append($@"
                <div class='section'>
                    <h2>Освіта</h2>
                    <div class='section-content'>{resume.Education}</div>
                </div>
            ");
        }
        
        if (!string.IsNullOrEmpty(resume.AdditionalInfo))
        {
            html.Append($@"
                <div class='section'>
                    <h2>Додаткова інформація</h2>
                    <div class='section-content'>{resume.AdditionalInfo}</div>
                </div>
            ");
        }
        
        html.Append($@"
                <div class='footer'>
                    Резюме створено: {resume.CreatedDate:dd.MM.yyyy}
                    {(resume.LastUpdated.HasValue ? $" | Оновлено: {resume.LastUpdated.Value:dd.MM.yyyy}" : "")}
                </div>
            </div>
        </body>
        </html>
        ");
        
        return html.ToString();
    }

    private async Task<byte[]> GeneratePdfWithPuppeteer(string html)
    {
        await new BrowserFetcher().DownloadAsync();
    
        using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox" }
        });
    
        using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
    
        var pdfOptions = new PdfOptions
        {
            Format = PaperFormat.A4,
            MarginOptions = new MarginOptions
            {
                Top = "10mm",
                Bottom = "10mm",
                Left = "10mm",
                Right = "10mm"
            },
            PrintBackground = true
        };
    
        return await page.PdfDataAsync(pdfOptions);
    }
}