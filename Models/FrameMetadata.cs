namespace BomLocalService.Models;

/// <summary>
/// Metadata for a single radar frame, stored alongside the frame image.
/// </summary>
public class FrameMetadata
{
    /// <summary>
    /// Frame index (0-6).
    /// </summary>
    public int FrameIndex { get; set; }
    
    /// <summary>
    /// Number of minutes ago this frame represents (40, 35, 30, 25, 20, 15, 10).
    /// </summary>
    public int MinutesAgo { get; set; }
}

