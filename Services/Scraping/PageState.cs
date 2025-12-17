namespace BomLocalService.Services.Scraping;

/// <summary>
/// Represents the current state of the page during scraping
/// </summary>
public enum PageState
{
    Initial,
    HomepageLoaded,
    SearchModalOpen,
    SearchResultsVisible,
    ForecastPageLoaded,
    RadarPageLoaded,
    MapReady,
    SlideshowPaused,
    Frame0Selected
}

