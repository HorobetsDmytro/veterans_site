using System.Text.Json;
using System.Text.Json.Serialization;
using veterans_site.Interfaces;
using veterans_site.Models;
using veterans_site.Services;

public class JoobleService : IJoobleService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IJobRepository _jobRepository;
    private readonly string _apiKey;
    public ILogger<JoobleService> _logger;
    
    public JoobleService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        IJobRepository jobRepository,
        ILogger<JoobleService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _jobRepository = jobRepository;
        _apiKey = _configuration["JoobleApiKey"];
        _logger = logger;
        
        _logger.LogInformation($"Jooble API Key: {_apiKey}");
        
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }
    
    public async Task<List<Job>> SearchJobsAsync(string keywords, string location, int count = 20)
    {
        try
        {
            _logger.LogInformation($"Searching jobs with keywords: {keywords}, location: {location}, count: {count}");
            
            var request = new
            {
                location = location ?? "Ukraine",
                limit = count
            };
            
            var url = $"https://jooble.org/api/{_apiKey}";
            _logger.LogInformation($"Sending request to: {url}");
        
            var response = await _httpClient.PostAsJsonAsync(url, request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Jooble API error: {response.StatusCode}, {errorContent}");
                return new List<Job>();
            }
            
            response.EnsureSuccessStatusCode();
            
            var joobleResponse = await response.Content.ReadFromJsonAsync<JoobleResponse>();
            
            if (joobleResponse == null || joobleResponse.Jobs == null)
                return new List<Job>();
            
            _logger.LogInformation($"API response received. Status: {response.StatusCode}, Jobs found: {joobleResponse?.Jobs?.Count ?? 0}");
            
            return joobleResponse.Jobs.Select(j => new Job
            {
                Title = j.Title,
                Company = j.Company ?? "Не вказано",
                Location = j.Location ?? "Не вказано",
                Description = j.Description,
                Salary = ParseSalary(j.Salary),
                PostedDate = ParseDate(j.Updated),
                IsExternal = true,
                ExternalId = j.Id.ToString(),
                ExternalUrl = j.Link,
                Category = DetermineCategory(j),
                JobType = DetermineJobType(j.Type)
            }).ToList();

        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in SearchJobsAsync: {ex.Message}\n{ex.StackTrace}");
            return new List<Job>();
        }
    }
    
    public async Task SyncJobsWithDatabaseAsync()
    {
        try {
            // Отримання вакансій за різними категоріями
            var keywords = new[] { "Veteran", "IT", "Medicine", "Education", "Production" };
            var allJobs = new List<Job>();
            
            foreach (var keyword in keywords)
            {
                var jobs = await SearchJobsAsync(keyword, null, 10);
                allJobs.AddRange(jobs);
            }
            
            // Проходимо по кожній вакансії
            foreach (var job in allJobs)
            {
                // Перевіряємо, чи вакансія вже є в базі
                var existingJob = await _jobRepository.GetByExternalIdAsync(job.ExternalId);
                
                if (existingJob == null)
                {
                    // Додаємо нову вакансію
                    await _jobRepository.AddAsync(job);
                }
                else
                {
                    // Оновлюємо існуючу вакансію
                    existingJob.Title = job.Title;
                    existingJob.Company = job.Company;
                    existingJob.Location = job.Location;
                    existingJob.Description = job.Description;
                    existingJob.Salary = job.Salary;
                    existingJob.ExternalUrl = job.ExternalUrl;
                    existingJob.Category = job.Category;
                    existingJob.JobType = job.JobType;
                    
                    await _jobRepository.UpdateAsync(existingJob);
                }
            }
        }
        catch (Exception ex) {
            // Логування помилки
            Console.WriteLine($"Помилка при синхронізації вакансій: {ex.Message}");
        }
    }
    
    private decimal? ParseSalary(string salaryText)
    {
        if (string.IsNullOrEmpty(salaryText)) return null;
        
        // Видаляємо всі нецифрові символи, крім крапки та коми
        string numericString = new string(salaryText.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
        
        // Заміняємо коми на крапки
        numericString = numericString.Replace(',', '.');
        
        if (decimal.TryParse(numericString, 
            System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, 
            out decimal result))
        {
            return result;
        }
        
        return null;
    }
    
    private DateTime ParseDate(string dateText)
    {
        if (DateTime.TryParse(dateText, out DateTime result))
            return result;
        
        return DateTime.Now;
    }
    
    private string DetermineCategory(JoobleJob job)
    {
        if (string.IsNullOrEmpty(job.Type)) return "Інше";
        
        // Простий алгоритм визначення категорії на основі назви та опису
        string content = $"{job.Title} {job.Description} {job.Type}".ToLower();
        
        if (content.Contains("it") || content.Contains("програміст") || content.Contains("розробник"))
            return "IT";
        else if (content.Contains("медик") || content.Contains("лікар") || content.Contains("медсестра"))
            return "Медицина";
        else if (content.Contains("вчитель") || content.Contains("викладач") || content.Contains("освіта"))
            return "Освіта";
        else if (content.Contains("інженер") || content.Contains("виробництво"))
            return "Виробництво";
        else
            return "Інше";
    }
    
    private JobType DetermineJobType(string type)
    {
        if (string.IsNullOrEmpty(type)) return JobType.FullTime;
        
        string lowerType = type.ToLower();
        
        if (lowerType.Contains("част") || lowerType.Contains("part"))
            return JobType.PartTime;
        else if (lowerType.Contains("фріланс") || lowerType.Contains("freelance"))
            return JobType.Freelance;
        else if (lowerType.Contains("тимчас") || lowerType.Contains("contract"))
            return JobType.Contract;
        else if (lowerType.Contains("стаж") || lowerType.Contains("intern"))
            return JobType.Internship;
        else
            return JobType.FullTime;
    }
}

// Клас для десеріалізації відповіді від Jooble API
public class JoobleResponse
{
    [JsonPropertyName("jobs")]
    public List<JoobleJob> Jobs { get; set; }
}

public class JoobleJob
{
    [JsonPropertyName("id")]
    public JsonElement Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("company")]
    public string Company { get; set; }
    
    [JsonPropertyName("location")]
    public string Location { get; set; }
    
    [JsonPropertyName("snippet")]
    public string Description { get; set; }
    
    [JsonPropertyName("salary")]
    public string Salary { get; set; }
    
    [JsonPropertyName("updated")]
    public string Updated { get; set; }
    
    [JsonPropertyName("link")]
    public string Link { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
}