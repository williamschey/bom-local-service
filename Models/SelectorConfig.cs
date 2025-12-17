namespace BomLocalService.Models;

/// <summary>
/// Configuration for a single selector with multiple fallback options
/// </summary>
public class SelectorConfig
{
    /// <summary>
    /// Human-readable name for this selector (for logging/debugging)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Array of CSS selectors to try in order (first match wins)
    /// </summary>
    public string[] Selectors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Timeout in milliseconds when waiting for this element
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Whether this element is required (throws if not found)
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Custom error message if element is not found (when Required=true)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

