namespace BomLocalService.Models;

/// <summary>
/// Represents a single frame from the radar slideshow (0-6).
/// </summary>
public class RadarFrame
{
    /// <summary>
    /// Frame index (0-6), where 0 is the oldest frame (40 minutes ago) and 6 is the newest (10 minutes ago).
    /// </summary>
    public int FrameIndex { get; set; }
    
    /// <summary>
    /// Full file system path to the frame image file (server-side only).
    /// Format: "{CacheDirectory}/{LocationKey}_{Timestamp}/frame_{FrameIndex}.png"
    /// Example: "/app/cache/Pomona_QLD_20251207_000906/frame_0.png"
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;
    
    /// <summary>
    /// URL endpoint to retrieve this frame image.
    /// Format: "/api/radar/{Suburb}/{State}/frame/{FrameIndex}"
    /// Example: "/api/radar/Pomona/QLD/frame/0"
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of minutes ago this frame represents (40, 35, 30, 25, 20, 15, 10).
    /// Frame 0 = 40 minutes ago, Frame 6 = 10 minutes ago.
    /// </summary>
    public int MinutesAgo { get; set; }
}

