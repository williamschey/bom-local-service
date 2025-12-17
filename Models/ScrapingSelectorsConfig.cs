namespace BomLocalService.Models;

/// <summary>
/// Configuration for all selectors used in scraping
/// </summary>
public class ScrapingSelectorsConfig
{
    public SelectorConfig SearchButton { get; set; } = new();
    public SelectorConfig SearchInput { get; set; } = new();
    public SelectorConfig SearchResultsList { get; set; } = new();
    public SelectorConfig SearchResultItem { get; set; } = new();
    public SelectorConfig LocationName { get; set; } = new();
    public SelectorConfig LocationDescription { get; set; } = new();
    public SelectorConfig ResultsTitle { get; set; } = new();
    public SelectorConfig RadarLink { get; set; } = new();
    public SelectorConfig MapCanvas { get; set; } = new();
    public SelectorConfig MapContainer { get; set; } = new();
    public SelectorConfig PlayPauseButton { get; set; } = new();
    public SelectorConfig PlayPauseLabel { get; set; } = new();
    public SelectorConfig FrameSegment { get; set; } = new();
    public SelectorConfig StepForwardButton { get; set; } = new();
    public SelectorConfig TimeDisplayLabel { get; set; } = new();
    public SelectorConfig ModalOverlay { get; set; } = new();
    public SelectorConfig WeatherMetadata { get; set; } = new();
}

