namespace BomLocalService.Models;

/// <summary>
/// Configuration for a single scraping step
/// </summary>
public class ScrapingStepConfig
{
    /// <summary>
    /// Whether this step is enabled (can be disabled without removing from workflow)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional parameters specific to this step
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}

