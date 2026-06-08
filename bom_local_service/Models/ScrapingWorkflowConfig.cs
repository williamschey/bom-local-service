namespace BomLocalService.Models;

/// <summary>
/// Configuration for a scraping workflow
/// </summary>
public class ScrapingWorkflowConfig
{
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, ScrapingStepConfig> Steps { get; set; } = new();
}

