namespace BomLocalService.Services.Scraping;

/// <summary>
/// Result of executing a scraping step
/// </summary>
public class ScrapingStepResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    
    public static ScrapingStepResult Successful() => new() { Success = true };
    public static ScrapingStepResult Failed(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}

