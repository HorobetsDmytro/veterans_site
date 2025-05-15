using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.Services;
using veterans_site.ViewModels;

namespace veterans_site.Controllers;

public class JobsController : Controller
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly ISavedJobRepository _savedJobRepository;
    private readonly IJoobleService _joobleService;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public JobsController(
        IJobRepository jobRepository,
        IJobApplicationRepository applicationRepository,
        ISavedJobRepository savedJobRepository,
        IJoobleService joobleService,
        UserManager<ApplicationUser> userManager)
    {
        _jobRepository = jobRepository;
        _applicationRepository = applicationRepository;
        _savedJobRepository = savedJobRepository;
        _joobleService = joobleService;
        _userManager = userManager;
    }
    
    public async Task<IActionResult> Index(string query, string location, string category, JobType? jobType, int page = 1)
    {
        int pageSize = 10;
        
        var joobleJobs = await _joobleService.SearchJobsAsync("Veteran", "Ukraine", 20);
        if (joobleJobs.Any())
        {
            foreach (var job in joobleJobs)
            {
                var existingJob = await _jobRepository.GetByExternalIdAsync(job.ExternalId);
                if (existingJob == null)
                {
                    await _jobRepository.AddAsync(job);
                }
            }
        }
        
        var jobs = await _jobRepository.SearchJobsAsync(query, location, category, jobType);
        
        var acceptedJobIds = await _applicationRepository.GetJobIdsWithStatusAsync(ApplicationStatus.Accepted);
        
        jobs = jobs.Where(job => !acceptedJobIds.Contains(job.Id)).ToList();
        
        var paginatedJobs = jobs.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        var userId = _userManager.GetUserId(User);
        
        if (!string.IsNullOrEmpty(userId))
        {
            foreach (var job in paginatedJobs)
            {
                job.IsSaved = await _savedJobRepository.IsJobSavedByUserAsync(userId, job.Id);
                job.IsApplied = await _applicationRepository.HasUserAppliedAsync(userId, job.Id);
            }
        }
        
        var viewModel = new JobsIndexViewModel
        {
            Jobs = paginatedJobs,
            Query = query,
            Location = location,
            Category = category,
            JobType = jobType,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)jobs.Count() / pageSize),
            Categories = await GetJobCategoriesAsync(),
            JobTypes = Enum.GetValues(typeof(JobType)).Cast<JobType>().ToList()
        };
        
        foreach (var job in viewModel.Jobs)
        {
            job.ApplicationsCount = await _applicationRepository.GetApplicationsCountAsync(job.Id);
        }
        
        return View(viewModel);
    }
    
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var job = await _jobRepository.GetByIdAsync(id);
    
        if (job == null)
            return NotFound();
    
        var userId = _userManager.GetUserId(User);
    
        if (!string.IsNullOrEmpty(userId))
        {
            job.IsSaved = await _savedJobRepository.IsJobSavedByUserAsync(userId, job.Id);
            job.IsApplied = await _applicationRepository.HasUserAppliedAsync(userId, job.Id);
        }
    
        job.ApplicationsCount = await _applicationRepository.GetApplicationsCountAsync(job.Id);
    
        return View(job);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Job job)
    {
        if (!ModelState.IsValid)
        {
            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    Console.WriteLine($"Поле {state.Key}: {error.ErrorMessage}");
                }
            }
        }
    
        job.PostedDate = DateTime.Now;
        job.IsExternal = false;
        
        if (!string.IsNullOrEmpty(job.Description))
        {
            job.Description = System.Text.RegularExpressions.Regex.Replace(job.Description, "<.*?>", string.Empty);
        }
    
        job.ExternalId = job.ExternalId ?? string.Empty;
        job.ExternalUrl = job.ExternalUrl ?? string.Empty;
    
        if (string.IsNullOrEmpty(job.Category))
        {
            job.Category = "Загальна";
        }
    
        try
        {
            await _jobRepository.AddAsync(job);
            TempData["Success"] = "Вакансію успішно додано!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка при збереженні вакансії: {ex.Message}");
            return View(job);
        }
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var job = await _jobRepository.GetByIdAsync(id);
        
        if (job == null)
            return NotFound();
        
        if (job.IsExternal)
            return BadRequest("Зовнішні вакансії не можна редагувати");
        
        return View(job);
    }
    
    [Authorize(Roles = "Admin")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, Job job)
{
    if (id != job.Id)
        return NotFound();
    
    try
    {
        if (!string.IsNullOrEmpty(job.Description))
        {
            job.Description = System.Text.RegularExpressions.Regex.Replace(job.Description, "<.*?>", string.Empty);
        }
        
        var dbContext = _jobRepository.GetDbContext();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                UPDATE Jobs 
                SET Title = @title, 
                    Company = @company, 
                    Location = @location, 
                    Salary = @salary, 
                    JobType = @jobType, 
                    Category = @category, 
                    Description = @description, 
                    ExpiryDate = @expiryDate
                WHERE Id = @id";
            
            var titleParam = command.CreateParameter();
            titleParam.ParameterName = "@title";
            titleParam.Value = job.Title;
            command.Parameters.Add(titleParam);
            
            var companyParam = command.CreateParameter();
            companyParam.ParameterName = "@company";
            companyParam.Value = job.Company;
            command.Parameters.Add(companyParam);
            
            var locationParam = command.CreateParameter();
            locationParam.ParameterName = "@location";
            locationParam.Value = job.Location;
            command.Parameters.Add(locationParam);
            
            var salaryParam = command.CreateParameter();
            salaryParam.ParameterName = "@salary";
            salaryParam.Value = job.Salary;
            command.Parameters.Add(salaryParam);
            
            var jobTypeParam = command.CreateParameter();
            jobTypeParam.ParameterName = "@jobType";
            jobTypeParam.Value = (int)job.JobType;
            command.Parameters.Add(jobTypeParam);
            
            var categoryParam = command.CreateParameter();
            categoryParam.ParameterName = "@category";
            categoryParam.Value = job.Category ?? string.Empty;
            command.Parameters.Add(categoryParam);
            
            var descriptionParam = command.CreateParameter();
            descriptionParam.ParameterName = "@description";
            descriptionParam.Value = job.Description ?? string.Empty;
            command.Parameters.Add(descriptionParam);
            
            var expiryDateParam = command.CreateParameter();
            expiryDateParam.ParameterName = "@expiryDate";
            expiryDateParam.Value = job.ExpiryDate.HasValue ? (object)job.ExpiryDate.Value : DBNull.Value;
            command.Parameters.Add(expiryDateParam);
            
            var idParam = command.CreateParameter();
            idParam.ParameterName = "@id";
            idParam.Value = id;
            command.Parameters.Add(idParam);
            
            var result = await command.ExecuteNonQueryAsync();
            
            if (result > 0)
            {
                TempData["Success"] = "Вакансію успішно оновлено!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["Error"] = "Вакансію не знайдено або не вдалося оновити.";
            }
        }
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("", $"Помилка при оновленні вакансії: {ex.Message}");
    }
    
    return View(job);
}
    
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var job = await _jobRepository.GetByIdAsync(id);
        
        if (job == null)
            return NotFound();
        
        return View(job);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var dbContext = _jobRepository.GetDbContext();
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
        
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"DELETE FROM JobApplications WHERE JobId = {id}";
                await command.ExecuteNonQueryAsync();
            
                command.CommandText = $"DELETE FROM SavedJobs WHERE JobId = {id}";
                await command.ExecuteNonQueryAsync();
            
                command.CommandText = $"DELETE FROM Jobs WHERE Id = {id}";
                var result = await command.ExecuteNonQueryAsync();
            
                if (result > 0)
                {
                    TempData["Success"] = "Вакансію успішно видалено!";
                }
                else
                {
                    TempData["Error"] = "Вакансію не знайдено або не вдалося видалити.";
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Помилка при видаленні вакансії: {ex.Message}";
        }
    
        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SaveJob(int jobId)
    {
        var userId = _userManager.GetUserId(User);
        
        if (await _savedJobRepository.IsJobSavedByUserAsync(userId, jobId))
            return Json(new { success = false, message = "Вакансія вже збережена" });
        
        var savedJob = new SavedJob
        {
            JobId = jobId,
            ApplicationUserId = userId,
            SavedDate = DateTime.Now
        };
        
        await _savedJobRepository.AddAsync(savedJob);
        
        return Json(new { success = true });
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UnsaveJob(int jobId)
    {
        var userId = _userManager.GetUserId(User);
        var savedJobs = await _savedJobRepository.GetSavedJobsByUserIdAsync(userId);
        var savedJob = savedJobs.FirstOrDefault(sj => sj.JobId == jobId);
        
        if (savedJob == null)
            return Json(new { success = false, message = "Вакансія не знайдена в збережених" });
        
        await _savedJobRepository.DeleteAsync(savedJob.Id);
        
        return Json(new { success = true });
    }
    
    [Authorize]
    public async Task<IActionResult> SavedJobs()
    {
        var userId = _userManager.GetUserId(User);
        var savedJobs = await _savedJobRepository.GetSavedJobsByUserIdAsync(userId);
        
        var jobs = savedJobs.Select(sj => sj.Job).ToList();
        
        return View(jobs);
    }
    
    private async Task<List<string>> GetJobCategoriesAsync()
    {
        var jobs = await _jobRepository.GetAllAsync();
        return jobs.Select(j => j.Category)
                  .Where(c => !string.IsNullOrEmpty(c))
                  .Distinct()
                  .OrderBy(c => c)
                  .ToList();
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult ImportFromJooble()
    {
        var viewModel = new JoobleImportViewModel
        {
            Keywords = "Veteran",
            Location = "Ukraine",
            Count = 20
        };
    
        return View(viewModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportFromJooble(JoobleImportViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var jobs = await _joobleService.SearchJobsAsync(
                viewModel.Keywords, viewModel.Location, viewModel.Count);
        
            int imported = 0;
        
            foreach (var job in jobs)
            {
                var existingJob = await _jobRepository.GetByExternalIdAsync(job.ExternalId);
            
                if (existingJob == null)
                {
                    await _jobRepository.AddAsync(job);
                    imported++;
                }
            }
        
            TempData["Success"] = $"Імпортовано {imported} нових вакансій";
            return RedirectToAction(nameof(Index));
        }
    
        return View(viewModel);
    }
    
    public async Task<IActionResult> MyJobs(string query = null, string location = null, 
        string category = null, JobType? jobType = null, int page = 1)
    {
        int pageSize = 10;

        var jobs = await _jobRepository.GetAllAsync();
        var adminJobs = jobs.Where(j => !j.IsExternal).AsQueryable();

        if (!string.IsNullOrEmpty(query))
        {
            adminJobs = adminJobs.Where(j => j.Title.Contains(query) || j.Description.Contains(query));
        }

        if (!string.IsNullOrEmpty(location))
        {
            adminJobs = adminJobs.Where(j => j.Location.Contains(location));
        }

        if (!string.IsNullOrEmpty(category))
        {
            adminJobs = adminJobs.Where(j => j.Category == category);
        }

        if (jobType.HasValue)
        {
            adminJobs = adminJobs.Where(j => j.JobType == jobType.Value);
        }

        var acceptedJobIds = await _applicationRepository.GetJobIdsWithStatusAsync(ApplicationStatus.Accepted);
    
        adminJobs = adminJobs.Where(job => !acceptedJobIds.Contains(job.Id));

        var totalCount = adminJobs.Count();

        var paginatedJobs = adminJobs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        foreach (var job in paginatedJobs)
        {
            job.ApplicationsCount = await _applicationRepository.GetApplicationsCountAsync(job.Id);
        }

        var viewModel = new JobsIndexViewModel
        {
            Jobs = paginatedJobs,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Categories = await GetJobCategoriesAsync(),
            Query = query,
            Location = location,
            Category = category,
            JobType = jobType
        };

        ViewBag.IsMyJobs = true;
        return View("Index", viewModel);
    }
}