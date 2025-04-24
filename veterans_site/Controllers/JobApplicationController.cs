using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.ViewModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using veterans_site.Data;

namespace veterans_site.Controllers;

[Authorize]
public class JobApplicationController : Controller
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly IResumeRepository _resumeRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly VeteranSupportDbContext _context;
    
    public JobApplicationController(
        IJobRepository jobRepository,
        IJobApplicationRepository applicationRepository,
        IResumeRepository resumeRepository,
        UserManager<ApplicationUser> userManager,
        VeteranSupportDbContext context)
    {
        _jobRepository = jobRepository;
        _applicationRepository = applicationRepository;
        _resumeRepository = resumeRepository;
        _userManager = userManager;
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> Apply(int jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        
        if (job == null)
            return NotFound();
            
        var userId = _userManager.GetUserId(User);
        
        if (await _applicationRepository.HasUserAppliedAsync(userId, jobId))
        {
            TempData["Error"] = "Ви вже відгукнулися на цю вакансію";
            return RedirectToAction("Details", "Jobs", new { id = jobId });
        }
        
        var resumes = await _resumeRepository.GetResumesByUserIdAsync(userId);
        
        var viewModel = new JobApplicationViewModel
        {
            JobId = jobId,
            Job = job,
            AvailableResumes = resumes.ToList()
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(JobApplicationViewModel viewModel)
    {
        viewModel.Job = await _jobRepository.GetByIdAsync(viewModel.JobId);
    
        ModelState.Remove("Job");
    
        if (!ModelState.IsValid)
        {
            var userId = _userManager.GetUserId(User);
            var resumes = await _resumeRepository.GetResumesByUserIdAsync(userId);
            viewModel.AvailableResumes = resumes.ToList();
            return View(viewModel);
        }
    
        var idUser = _userManager.GetUserId(User);
    
        if (await _applicationRepository.HasUserAppliedAsync(idUser, viewModel.JobId))
        {
            TempData["Error"] = "Ви вже відгукнулися на цю вакансію";
            return RedirectToAction("Details", "Jobs", new { id = viewModel.JobId });
        }
    
        var application = new JobApplication
        {
            JobId = viewModel.JobId,
            ApplicationUserId = idUser,
            ResumeId = viewModel.ResumeId,
            CoverLetter = viewModel.CoverLetter,
            ApplicationDate = DateTime.Now,
            Status = ApplicationStatus.Pending
        };
    
        await _applicationRepository.AddAsync(application);
    
        TempData["Success"] = "Вашу заявку успішно надіслано";
        return RedirectToAction("Details", "Jobs", new { id = viewModel.JobId });
    }
    
    [HttpGet]
    public async Task<IActionResult> MyApplications()
    {
        var userId = _userManager.GetUserId(User);
        var applications = await _applicationRepository.GetApplicationsByUserIdAsync(userId);
        
        return View(applications);
    }
    
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var application = await _applicationRepository.GetByIdAsync(id);
    
        if (application == null)
            return NotFound();
    
        var userId = _userManager.GetUserId(User);
    
        if (application.ApplicationUserId != userId && !User.IsInRole("Admin"))
            return Forbid();
    
        if (application.Job == null)
            application.Job = await _jobRepository.GetByIdAsync(application.JobId);
    
        if (application.Resume == null && application.ResumeId.HasValue)
            application.Resume = await _resumeRepository.GetByIdAsync(application.ResumeId.Value);
    
        return View(application);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> JobApplications(int jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
    
        if (job == null)
            return NotFound();
        
        var applications = await _applicationRepository.GetApplicationsByJobIdAsync(jobId);
    
        foreach (var application in applications)
        {
            if (application.Resume == null && application.ResumeId.HasValue)
            {
                application.Resume = await _context.Resumes.FindAsync(application.ResumeId.Value);
            }
        }
    
        var viewModel = new JobApplicationsViewModel
        {
            Job = job,
            Applications = applications.ToList()
        };
    
        return View(viewModel);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, ApplicationStatus status, string statusNote)
    {
        var application = await _applicationRepository.GetByIdAsync(id);
        
        if (application == null)
            return NotFound();
            
        application.Status = status;
        application.StatusNote = statusNote;
        
        await _applicationRepository.UpdateAsync(application);
        
        return RedirectToAction("JobApplications", new { jobId = application.JobId });
    }
    
    [HttpPost]
    public async Task<IActionResult> Withdraw(int id)
    {
        var application = await _applicationRepository.GetByIdAsync(id);
        
        if (application == null)
            return NotFound();
            
        var userId = _userManager.GetUserId(User);
        
        if (application.ApplicationUserId != userId)
            return Forbid();
            
        await _applicationRepository.DeleteAsync(id);
        
        TempData["Success"] = "Заявку успішно відкликано";
        return RedirectToAction("MyApplications");
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> ExportToExcel(int jobId, List<string> statuses)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        
        if (job == null)
            return NotFound();
            
        var applications = await _applicationRepository.GetApplicationsByJobIdAsync(jobId);
        
        if (statuses != null && statuses.Any())
        {
            var statusEnums = statuses.Select(s => Enum.Parse<ApplicationStatus>(s)).ToList();
            applications = applications.Where(a => statusEnums.Contains(a.Status)).ToList();
        }
        
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Заявки");
            
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "ПІБ кандидата";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Телефон";
            worksheet.Cell(1, 5).Value = "Дата заявки";
            worksheet.Cell(1, 6).Value = "Статус";
            worksheet.Cell(1, 7).Value = "Коментар";
            
            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            
            int row = 2;
            foreach (var app in applications)
            {
                worksheet.Cell(row, 1).Value = app.Id;
                worksheet.Cell(row, 2).Value = $"{app.User.FirstName} {app.User.LastName}";
                worksheet.Cell(row, 3).Value = app.User.Email;
                worksheet.Cell(row, 4).Value = app.User.PhoneNumber ?? "Не вказано";
                worksheet.Cell(row, 5).Value = app.ApplicationDate;
                
                string status = app.Status switch
                {
                    ApplicationStatus.Pending => "В очікуванні",
                    ApplicationStatus.Reviewed => "Розглянуто",
                    ApplicationStatus.InterviewInvited => "Співбесіда",
                    ApplicationStatus.Accepted => "Прийнято",
                    ApplicationStatus.Rejected => "Відхилено",
                    _ => "Невідомо"
                };
                
                worksheet.Cell(row, 6).Value = status;
                worksheet.Cell(row, 7).Value = app.StatusNote ?? "";
                
                row++;
            }
            
            worksheet.Columns().AdjustToContents();
            
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                stream.Position = 0;
                
                return File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Заявки_на_вакансію_{job.Title}_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> AllApplications(string jobFilter = "", string statusFilter = "")
    {
        var allApplications = await _context.JobApplications
            .Include(a => a.Job)
            .Include(a => a.User)
            .Include(a => a.Resume)
            .ToListAsync();
        
        var allJobs = await _jobRepository.GetAllAsync();
    
        if (!string.IsNullOrEmpty(jobFilter) && int.TryParse(jobFilter, out int jobId))
        {
            allApplications = allApplications.Where(a => a.JobId == jobId).ToList();
        }
    
        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<ApplicationStatus>(statusFilter, out var status))
        {
            allApplications = allApplications.Where(a => a.Status == status).ToList();
        }
    
        foreach (var application in allApplications)
        {
            if (application.Resume == null && application.ResumeId.HasValue)
            {
                application.Resume = _context.Resumes.Local.FirstOrDefault(r => r.Id == application.ResumeId);
            
                if (application.Resume == null)
                {
                    application.Resume = await _context.Resumes.FindAsync(application.ResumeId);
                }
            }
        }
    
        var viewModel = new AllJobApplicationsViewModel
        {
            Applications = allApplications,
            Jobs = allJobs,
            JobFilter = jobFilter,
            StatusFilter = statusFilter
        };
    
        return View(viewModel);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> ExportAllToExcel(string jobFilter = "", string statusFilter = "", List<string> statuses = null)
    {
        var applications = await _applicationRepository.GetAllApplicationsAsync();
        
        if (!string.IsNullOrEmpty(jobFilter) && int.TryParse(jobFilter, out int jobId))
        {
            applications = applications.Where(a => a.JobId == jobId).ToList();
        }
        
        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<ApplicationStatus>(statusFilter, out var status))
        {
            applications = applications.Where(a => a.Status == status).ToList();
        }
        
        if (statuses != null && statuses.Any())
        {
            var statusEnums = statuses.Select(s => Enum.Parse<ApplicationStatus>(s)).ToList();
            applications = applications.Where(a => statusEnums.Contains(a.Status)).ToList();
        }
        
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Всі заявки");
            
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Вакансія";
            worksheet.Cell(1, 3).Value = "ПІБ кандидата";
            worksheet.Cell(1, 4).Value = "Email";
            worksheet.Cell(1, 5).Value = "Телефон";
            worksheet.Cell(1, 6).Value = "Дата заявки";
            worksheet.Cell(1, 7).Value = "Статус";
            worksheet.Cell(1, 8).Value = "Коментар";
            
            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            
            int row = 2;
            foreach (var app in applications)
            {
                worksheet.Cell(row, 1).Value = app.Id;
                worksheet.Cell(row, 2).Value = app.Job.Title;
                worksheet.Cell(row, 3).Value = $"{app.User.FirstName} {app.User.LastName}";
                worksheet.Cell(row, 4).Value = app.User.Email;
                worksheet.Cell(row, 5).Value = app.Resume.Phone ?? "Не вказано";
                worksheet.Cell(row, 6).Value = app.ApplicationDate;
                
                string applicationStatus = app.Status switch
                {
                    ApplicationStatus.Pending => "В очікуванні",
                    ApplicationStatus.Reviewed => "Розглянуто",
                    ApplicationStatus.InterviewInvited => "Співбесіда",
                    ApplicationStatus.Accepted => "Прийнято",
                    ApplicationStatus.Rejected => "Відхилено",
                    _ => "Невідомо"
                };
                
                worksheet.Cell(row, 7).Value = applicationStatus;
                worksheet.Cell(row, 8).Value = app.StatusNote ?? "";
                
                row++;
            }
            
            worksheet.Columns().AdjustToContents();
            
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                stream.Position = 0;
                
                return File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Всі_заявки_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
    }
}