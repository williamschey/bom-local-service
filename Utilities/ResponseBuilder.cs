using BomLocalService.Models;

namespace BomLocalService.Utilities;

public static class ResponseBuilder
{
    /// <summary>
    /// Creates a RadarResponse from a cache folder path, frames, and metadata
    /// </summary>
    public static RadarResponse CreateRadarResponse(
        string cacheFolderPath, 
        List<RadarFrame> frames,
        LastUpdatedInfo? metadata = null,
        string? suburb = null,
        string? state = null)
    {
        var folderInfo = new DirectoryInfo(cacheFolderPath);
        var lastWriteTime = folderInfo.Exists 
            ? folderInfo.LastWriteTime 
            : DateTime.UtcNow;

        // Generate URLs for each frame if suburb and state are provided
        if (!string.IsNullOrEmpty(suburb) && !string.IsNullOrEmpty(state))
        {
            var encodedSuburb = Uri.EscapeDataString(suburb);
            var encodedState = Uri.EscapeDataString(state);
            foreach (var frame in frames)
            {
                frame.ImageUrl = $"/api/radar/{encodedSuburb}/{encodedState}/frame/{frame.FrameIndex}";
            }
        }

        if (metadata == null)
        {
            return new RadarResponse
            {
                Frames = frames,
                LastUpdated = lastWriteTime,
                ObservationTime = DateTime.UtcNow,
                ForecastTime = DateTime.UtcNow
            };
        }

        return new RadarResponse
        {
            Frames = frames,
            LastUpdated = lastWriteTime,
            ObservationTime = metadata.ObservationTime,
            ForecastTime = metadata.ForecastTime,
            WeatherStation = metadata.WeatherStation,
            Distance = metadata.Distance
        };
    }
    
    /// <summary>
    /// Legacy method for backward compatibility - creates response with single frame
    /// </summary>
    [Obsolete("Use CreateRadarResponse with cacheFolderPath and frames")]
    public static RadarResponse CreateRadarResponse(
        string imagePath, 
        LastUpdatedInfo? metadata = null)
    {
        var lastWriteTime = File.Exists(imagePath) 
            ? File.GetLastWriteTime(imagePath) 
            : DateTime.UtcNow;

        if (metadata == null)
        {
            return new RadarResponse
            {
                Frames = new List<RadarFrame>(),
                LastUpdated = lastWriteTime,
                ObservationTime = DateTime.UtcNow,
                ForecastTime = DateTime.UtcNow
            };
        }

        return new RadarResponse
        {
            Frames = new List<RadarFrame>(),
            LastUpdated = lastWriteTime,
            ObservationTime = metadata.ObservationTime,
            ForecastTime = metadata.ForecastTime,
            WeatherStation = metadata.WeatherStation,
            Distance = metadata.Distance
        };
    }
}

