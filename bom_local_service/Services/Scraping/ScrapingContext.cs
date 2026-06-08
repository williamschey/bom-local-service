using BomLocalService.Models;
using Microsoft.Playwright;

namespace BomLocalService.Services.Scraping;

/// <summary>
/// Context shared between scraping steps
/// </summary>
public class ScrapingContext
{
    public IPage Page { get; set; } = null!;
    public string Suburb { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CacheFolderPath { get; set; } = string.Empty;
    public string DebugFolder { get; set; } = string.Empty;
    public List<(string type, string text, DateTime timestamp)> ConsoleMessages { get; set; } = new();
    public List<(string method, string url, int? status, string resourceType, DateTime timestamp)> NetworkRequests { get; set; } = new();
    
    // Page state tracking
    public PageState CurrentState { get; set; } = PageState.Initial;
    public HashSet<string> CompletedSteps { get; set; } = new();
    
    // Shared state between steps
    public ILocator? SearchButton { get; set; }
    public ILocator? SearchInput { get; set; }
    public List<(string name, string desc, string fullText)>? SearchResults { get; set; }
    public int? SelectedResultIndex { get; set; }
    public ILocator? RadarLink { get; set; }
    public ILocator? MapContainer { get; set; }
    public Clip? MapBoundingBox { get; set; }
    public LastUpdatedInfo? LastUpdatedInfo { get; set; }
    public List<(int index, int minutesAgo)>? FrameInfo { get; set; }
    public List<RadarFrame> Frames { get; set; } = new();
    
    // State validation helpers
    public bool IsHomepageLoaded => CurrentState >= PageState.HomepageLoaded;
    public bool IsSearchModalOpen => CurrentState >= PageState.SearchModalOpen;
    public bool IsForecastPageLoaded => CurrentState >= PageState.ForecastPageLoaded;
    public bool IsRadarPageLoaded => CurrentState >= PageState.RadarPageLoaded;
    public bool IsMapReady => CurrentState >= PageState.MapReady;
}

