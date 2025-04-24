namespace veterans_site.Services;

public class JoobleBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JoobleBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    
    public JoobleBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<JoobleBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Служба синхронізації вакансій запущена.");
        
        // Отримуємо період синхронізації з конфігурації (наприклад, кожні 12 годин)
        int syncInterval = _configuration.GetValue<int>("JobSync:IntervalHours", 12);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Початок синхронізації вакансій з Jooble API.");
                
                // Створюємо новий скоуп для DI
                using (var scope = _serviceProvider.CreateScope())
                {
                    // Отримуємо сервіс Jooble
                    var joobleService = scope.ServiceProvider.GetRequiredService<IJoobleService>();
                    
                    // Викликаємо метод синхронізації
                    await joobleService.SyncJobsWithDatabaseAsync();
                }
                
                _logger.LogInformation("Синхронізація вакансій успішно завершена.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при синхронізації вакансій.");
            }
            
            // Очікуємо певний час перед наступною синхронізацією
            await Task.Delay(TimeSpan.FromHours(syncInterval), stoppingToken);
        }
    }
}