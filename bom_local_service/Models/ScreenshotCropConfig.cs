namespace BomLocalService.Models;

/// <summary>
/// Configuration for cropping screenshots to avoid map controls and overlays.
/// All values are relative to the map container's bounding box.
/// </summary>
public class ScreenshotCropConfig
{
    /// <summary>
    /// X offset in pixels from the left edge of the map container.
    /// Default: 0 (start at container's left edge).
    /// </summary>
    public int X { get; set; } = 0;

    /// <summary>
    /// Y offset in pixels from the top edge of the map container.
    /// Default: 0 (start at container's top edge).
    /// </summary>
    public int Y { get; set; } = 0;

    /// <summary>
    /// Right offset in pixels from the right edge of the map container.
    /// How much to cut off from the right side.
    /// Default: 0 (no cut from right edge).
    /// </summary>
    public int RightOffset { get; set; } = 0;

    /// <summary>
    /// Height of the crop area in pixels.
    /// If null, uses the full container height minus Y offset.
    /// </summary>
    public int? Height { get; set; }
}

