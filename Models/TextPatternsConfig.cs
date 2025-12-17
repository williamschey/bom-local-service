namespace BomLocalService.Models;

/// <summary>
/// Text patterns and regex configurations for parsing page content
/// </summary>
public class TextPatternsConfig
{
    public string ResultsCountPattern { get; set; } = @"(\d+)\s+of\s+(\d+)";
    public string TimestampPattern { get; set; } = @"(?:[A-Za-z]+\s+)?\d{1,2}\s+[A-Za-z]{3},?\s+\d{1,2}:\d{2}\s+(?:am|pm)";
    public string ObservationTimePattern { get; set; } = @"Observations:\s*(\d+)\s*minutes?\s*ago";
    public string ForecastTimePattern { get; set; } = @"Forecast:\s*(\d+)\s*minutes?\s+ago";
    public string ForecastHourAgoPattern { get; set; } = @"Forecast:\s*an\s+hour\s+ago";
    public string WeatherStationPattern { get; set; } = @"at\s+([^,]+)\s+weather\s+station";
    public string DistancePattern { get; set; } = @"(\d+)\s*km\s+from";
    
    public Dictionary<string, string> ExpectedTexts { get; set; } = new()
    {
        ["PlayButtonLabel"] = "Play",
        ["PauseButtonLabel"] = "Pause",
        ["RadarLinkText"] = "Rain radar and weather map"
    };
}

