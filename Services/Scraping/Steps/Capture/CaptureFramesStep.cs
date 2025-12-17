using BomLocalService.Models;
using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using BomLocalService.Utilities;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace BomLocalService.Services.Scraping.Steps.Capture;

public class CaptureFramesStep : BaseScrapingStep
{
    private readonly ICacheService _cacheService;
    private readonly int _tileRenderWaitMs;
    private readonly ScreenshotCropConfig _cropConfig;
    
    public override string Name => "CaptureFrames";
    public override string[] Prerequisites => new[] { "ExtractMetadata", "CalculateMapBounds" };
    
    public CaptureFramesStep(
        ILogger<CaptureFramesStep> logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration,
        ICacheService cacheService)
        : base(logger, selectorService, debugService, configuration)
    {
        _cacheService = cacheService;
        _tileRenderWaitMs = configuration.GetValue<int>("Screenshot:TileRenderWaitMs", 5000);
        
        var cropSection = configuration.GetSection("Screenshot:Crop");
        _cropConfig = new ScreenshotCropConfig
        {
            X = cropSection.GetValue<int>("X", 0),
            Y = cropSection.GetValue<int>("Y", 0),
            RightOffset = cropSection.GetValue<int>("RightOffset", 0),
            Height = cropSection.GetValue<int?>("Height")
        };
    }
    
    public override bool CanExecute(ScrapingContext context)
    {
        return context.MapBoundingBox != null && context.MapContainer != null;
    }
    
    public override async Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(context.CacheFolderPath);
            Logger.LogInformation("Step {Step}: Using cache folder: {Path}", Name, context.CacheFolderPath);
            
            var frameCount = CacheHelper.GetFrameCountForDataType(Configuration, CachedDataType.Radar);
            
            _cacheService.RecordUpdateProgressByFolder(context.CacheFolderPath, CacheUpdatePhase.CapturingFrames, 0, frameCount);
            
            var frames = new List<RadarFrame>();
            var stepForwardButton = SelectorService.GetLocator(context.Page, Selectors.StepForwardButton);
            int? previousMinutesAgo = null;
            
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                Logger.LogInformation("Step {Step}: Capturing frame {FrameIndex} of {FrameCount}", Name, frameIndex, frameCount);
                
                await context.Page.WaitForTimeoutAsync(_tileRenderWaitMs);
                
                // Small wait to ensure display label is stable after frame change/selection
                // This is especially important for frame 0 which was just selected in ResetToFirstFrame
                await context.Page.WaitForTimeoutAsync(300);
                
                // Try extracting with a retry in case the label is still updating
                var minutesAgo = await ExtractMinutesAgoFromDisplayAsync(context.Page);
                if (minutesAgo == null)
                {
                    // Retry once after a short wait in case label was updating
                    await context.Page.WaitForTimeoutAsync(200);
                    minutesAgo = await ExtractMinutesAgoFromDisplayAsync(context.Page);
                }
                if (minutesAgo == null && context.FrameInfo != null && frameIndex < context.FrameInfo.Count)
                {
                    var (_, defaultMinutesAgo) = context.FrameInfo[frameIndex];
                    minutesAgo = defaultMinutesAgo;
                    Logger.LogWarning("Step {Step}: Failed to extract minutes from display label for frame {FrameIndex}, using default: {MinutesAgo}", Name, frameIndex, minutesAgo);
                }
                
                if (frameIndex > 0 && previousMinutesAgo.HasValue && minutesAgo == previousMinutesAgo.Value)
                {
                    Logger.LogWarning("Step {Step}: Frame {FrameIndex} has same minutesAgo ({MinutesAgo}) as previous frame. Waiting for display to update...", Name, frameIndex, minutesAgo);
                    await WaitForDisplayLabelToChangeAsync(context.Page, previousMinutesAgo.Value);
                    minutesAgo = await ExtractMinutesAgoFromDisplayAsync(context.Page);
                    if (minutesAgo == null || minutesAgo == previousMinutesAgo.Value)
                    {
                        if (context.FrameInfo != null && frameIndex < context.FrameInfo.Count)
                        {
                            var (_, defaultMinutesAgo) = context.FrameInfo[frameIndex];
                            minutesAgo = defaultMinutesAgo;
                            Logger.LogWarning("Step {Step}: Display label did not update for frame {FrameIndex}, using calculated default: {MinutesAgo}", Name, frameIndex, minutesAgo);
                        }
                    }
                }
                
                var radarFolder = FilePathHelper.GetDataTypeFolderPath(context.CacheFolderPath, CachedDataType.Radar);
                if (!Directory.Exists(radarFolder))
                {
                    Directory.CreateDirectory(radarFolder);
                }
                
                var framePath = FilePathHelper.GetFrameFilePath(context.CacheFolderPath, CachedDataType.Radar, frameIndex);
                await CaptureMapScreenshotAsync(context.Page, context.MapContainer!, framePath, context.MapBoundingBox!);
                
                frames.Add(new RadarFrame
                {
                    FrameIndex = frameIndex,
                    ImagePath = framePath,
                    MinutesAgo = minutesAgo ?? 0
                });
                
                previousMinutesAgo = minutesAgo;
                
                Logger.LogInformation("Step {Step}: Frame {FrameIndex} saved: {Path} ({MinutesAgo} minutes ago)", 
                    Name, frameIndex, framePath, minutesAgo ?? 0);
                
                _cacheService.RecordUpdateProgressByFolder(context.CacheFolderPath, CacheUpdatePhase.CapturingFrames, frameIndex + 1, frameCount);
                
                await SaveDebugAsync(context, 15 + frameIndex, $"frame_{frameIndex}_captured", cancellationToken);
                
                if (frameIndex < frameCount - 1)
                {
                    await DismissModalOverlaysAsync(context.Page);
                    
                    var currentMinutesAgo = await ExtractMinutesAgoFromDisplayAsync(context.Page);
                    
                    await stepForwardButton.ClickAsync(new LocatorClickOptions { Force = true });
                    
                    if (currentMinutesAgo.HasValue)
                    {
                        await WaitForDisplayLabelToChangeAsync(context.Page, currentMinutesAgo.Value);
                    }
                    else
                    {
                        await context.Page.WaitForTimeoutAsync(500);
                    }
                }
            }
            
            Logger.LogInformation("Step {Step}: All {FrameCount} frames captured successfully", Name, frameCount);
            
            _cacheService.RecordUpdateProgressByFolder(context.CacheFolderPath, CacheUpdatePhase.Saving);
            
            if (context.LastUpdatedInfo != null)
            {
                await _cacheService.SaveMetadataAsync(context.CacheFolderPath, context.LastUpdatedInfo, cancellationToken);
            }
            await _cacheService.SaveFramesMetadataAsync(context.CacheFolderPath, CachedDataType.Radar, frames, cancellationToken);
            
            context.Frames = frames;
            
            return ScrapingStepResult.Successful();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Step {Step} failed", Name);
            await SaveErrorDebugAsync(context, $"Failed to capture frames: {ex.Message}", cancellationToken);
            return ScrapingStepResult.Failed($"Failed to capture frames: {ex.Message}");
        }
    }
    
    private async Task<int?> ExtractMinutesAgoFromDisplayAsync(IPage page)
    {
        try
        {
            var timeLabelLocator = SelectorService.GetLocator(page, Selectors.TimeDisplayLabel);
            var timeLabel = await timeLabelLocator.TextContentAsync();
            if (string.IsNullOrEmpty(timeLabel))
            {
                Logger.LogDebug("Time display label is empty");
                return null;
            }
            
            var trimmedLabel = timeLabel.Trim();
            Logger.LogInformation("Extracting minutes from display label: '{Label}'", trimmedLabel);
            
            // Parse timestamp format (current BOM website format): "Wednesday 17 Dec, 11:05 pm" or "17 Dec, 11:05 pm"
            Logger.LogInformation("Trying timestamp pattern: '{Pattern}'", TextPatterns.TimestampPattern);
            var timestampMatch = Regex.Match(trimmedLabel, TextPatterns.TimestampPattern, RegexOptions.IgnoreCase);
            if (timestampMatch.Success)
            {
                var timestampStr = timestampMatch.Groups[0].Value;
                Logger.LogInformation("Matched timestamp pattern: '{Timestamp}' from label: '{Label}'", timestampStr, trimmedLabel);
                if (TryParseTimestamp(timestampStr, out var timestamp))
                {
                    var minutesAgo = (int)(DateTime.UtcNow - timestamp).TotalMinutes;
                    Logger.LogInformation("Parsed timestamp: {Timestamp} UTC, calculated minutes ago: {Minutes}", timestamp, minutesAgo);
                    if (minutesAgo >= 0 && minutesAgo <= 120) // Reasonable range: 0-2 hours
                    {
                        Logger.LogInformation("Successfully calculated minutes ago from timestamp: {Minutes}", minutesAgo);
                        return minutesAgo;
                    }
                    else
                    {
                        Logger.LogWarning("Calculated minutes ago ({Minutes}) outside reasonable range (0-120)", minutesAgo);
                    }
                }
                else
                {
                    Logger.LogWarning("Failed to parse timestamp string: '{Timestamp}'", timestampStr);
                }
            }
            else
            {
                Logger.LogWarning("Display label did not match timestamp pattern. Label: '{Label}', Pattern: '{Pattern}'", trimmedLabel, TextPatterns.TimestampPattern);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to extract minutes from display label");
            return null;
        }
    }
    
    private bool TryParseTimestamp(string timestampStr, out DateTime timestamp)
    {
        timestamp = DateTime.MinValue;
        
        try
        {
            // Try common Australian date formats
            // Format: "Wednesday 17 Dec, 11:05 pm" or "17 Dec, 11:05 pm"
            var formats = new[]
            {
                "dddd d MMM, h:mm tt",      // Wednesday 17 Dec, 11:05 pm
                "d MMM, h:mm tt",           // 17 Dec, 11:05 pm
                "dddd d MMM, hh:mm tt",     // Wednesday 17 Dec, 11:05 pm (with leading zero)
                "d MMM, hh:mm tt",          // 17 Dec, 11:05 pm (with leading zero)
                "dddd dd MMM, h:mm tt",     // Wednesday 17 Dec, 11:05 pm (with leading zero day)
                "dd MMM, h:mm tt"           // 17 Dec, 11:05 pm (with leading zero day)
            };
            
            var culture = new System.Globalization.CultureInfo("en-AU");
            
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(timestampStr, format, culture, 
                    System.Globalization.DateTimeStyles.AssumeLocal, out timestamp))
                {
                    // If year is not specified, assume current year
                    if (timestamp.Year == 1)
                    {
                        timestamp = new DateTime(DateTime.Now.Year, timestamp.Month, timestamp.Day, 
                            timestamp.Hour, timestamp.Minute, timestamp.Second);
                    }
                    
                    // If the parsed time is in the future (likely same day next year), adjust
                    if (timestamp > DateTime.Now && timestamp < DateTime.Now.AddDays(1))
                    {
                        // Already correct
                    }
                    else if (timestamp > DateTime.Now)
                    {
                        // Likely parsed as next year, adjust to this year
                        timestamp = timestamp.AddYears(-1);
                    }
                    
                    return true;
                }
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task WaitForDisplayLabelToChangeAsync(IPage page, int currentMinutesAgo, int maxWaitMs = 5000)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < maxWaitMs)
            {
                var newMinutesAgo = await ExtractMinutesAgoFromDisplayAsync(page);
                if (newMinutesAgo.HasValue && newMinutesAgo.Value != currentMinutesAgo)
                {
                    return;
                }
                await page.WaitForTimeoutAsync(200);
            }
            Logger.LogDebug("Display label did not change from {CurrentMinutesAgo} within {MaxWaitMs}ms", currentMinutesAgo, maxWaitMs);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error waiting for display label to change");
        }
    }
    
    private async Task DismissModalOverlaysAsync(IPage page)
    {
        try
        {
            var hasModal = await page.EvaluateAsync<bool>(JavaScriptTemplates.CheckModalOverlay);
            
            if (!hasModal)
            {
                return;
            }
            
            Logger.LogDebug("Modal overlay detected, dismissing");
            
            await page.Keyboard.PressAsync("Escape");
            await page.WaitForTimeoutAsync(200);
            
            var stillVisible = await page.EvaluateAsync<bool>(JavaScriptTemplates.CheckModalStillVisible);
            
            if (stillVisible)
            {
                try
                {
                    var mapContainer = SelectorService.GetLocator(page, Selectors.MapContainer);
                    await mapContainer.ClickAsync(new LocatorClickOptions { Force = true });
                    await page.WaitForTimeoutAsync(100);
                }
                catch
                {
                    // Ignore if click fails
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error dismissing modal overlay, continuing");
        }
    }
    
    private async Task CaptureMapScreenshotAsync(IPage page, ILocator mapContainer, string outputPath, Clip containerClip)
    {
        if (containerClip == null || containerClip.Width <= 0 || containerClip.Height <= 0)
        {
            Logger.LogError("Invalid container bounds: X={X}, Y={Y}, Width={Width}, Height={Height}", 
                containerClip?.X ?? 0, containerClip?.Y ?? 0, containerClip?.Width ?? 0, containerClip?.Height ?? 0);
            throw new Exception($"Invalid container bounds: {containerClip?.Width ?? 0}x{containerClip?.Height ?? 0}");
        }
        
        Clip cropArea;
        try
        {
            cropArea = CalculateCropArea(containerClip);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to calculate crop area, using full container bounds");
            cropArea = containerClip;
        }
        
        var viewportSize = page.ViewportSize;
        int? viewportWidth = viewportSize?.Width;
        int? viewportHeight = viewportSize?.Height;
        
        if (viewportWidth == null || viewportHeight == null)
        {
            try
            {
                var viewportJson = await page.EvaluateAsync<string>(JavaScriptTemplates.GetViewportSize);
                if (!string.IsNullOrEmpty(viewportJson))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(viewportJson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("width", out var widthProp) && root.TryGetProperty("height", out var heightProp))
                    {
                        if (widthProp.TryGetInt32(out var width) && heightProp.TryGetInt32(out var height))
                        {
                            viewportWidth = width;
                            viewportHeight = height;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to get viewport size from page evaluation");
            }
            
            if (viewportWidth == null || viewportHeight == null)
            {
                viewportWidth = (int)containerClip.Width;
                viewportHeight = (int)containerClip.Height;
            }
        }
        
        if (viewportWidth.HasValue && viewportHeight.HasValue)
        {
            if (cropArea.X < 0)
            {
                cropArea = new Clip { X = 0, Y = cropArea.Y, Width = cropArea.Width + cropArea.X, Height = cropArea.Height };
            }
            if (cropArea.Y < 0)
            {
                cropArea = new Clip { X = cropArea.X, Y = 0, Width = cropArea.Width, Height = cropArea.Height + cropArea.Y };
            }
            
            if (cropArea.X + cropArea.Width > viewportWidth.Value)
            {
                var newWidth = viewportWidth.Value - cropArea.X;
                cropArea = new Clip { X = cropArea.X, Y = cropArea.Y, Width = newWidth, Height = cropArea.Height };
            }
            if (cropArea.Y + cropArea.Height > viewportHeight.Value)
            {
                var newHeight = viewportHeight.Value - cropArea.Y;
                cropArea = new Clip { X = cropArea.X, Y = cropArea.Y, Width = cropArea.Width, Height = newHeight };
            }
        }
        
        if (cropArea.Width <= 0 || cropArea.Height <= 0)
        {
            Logger.LogError("Invalid crop dimensions after validation: {Width}x{Height}, using full container", cropArea.Width, cropArea.Height);
            cropArea = containerClip;
        }
        
        if (cropArea.Width <= 0 || cropArea.Height <= 0)
        {
            throw new Exception($"Cannot create valid crop area. Container: {containerClip.Width}x{containerClip.Height}, Viewport: {viewportWidth ?? 0}x{viewportHeight ?? 0}");
        }
        
        try
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 5000 });
        }
        catch
        {
            // Continue if network idle timeout
        }
        
        await DismissModalOverlaysAsync(page);
        
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = outputPath,
            Clip = cropArea,
            Type = ScreenshotType.Png,
            Animations = ScreenshotAnimations.Disabled
        });
        
        Logger.LogDebug("Screenshot saved: {Path} (crop: {X},{Y} {Width}x{Height})", 
            outputPath, cropArea.X, cropArea.Y, cropArea.Width, cropArea.Height);
    }
    
    private Clip CalculateCropArea(Clip containerClip)
    {
        var x = containerClip.X + _cropConfig.X;
        var y = containerClip.Y + _cropConfig.Y;
        var width = Math.Max(0, containerClip.Width - _cropConfig.X - _cropConfig.RightOffset);
        var height = _cropConfig.Height ?? Math.Max(0, containerClip.Height - _cropConfig.Y);
        
        if (x < containerClip.X || y < containerClip.Y)
        {
            x = containerClip.X;
            y = containerClip.Y;
        }
        
        var maxWidth = containerClip.Width - (x - containerClip.X);
        var maxHeight = containerClip.Height - (y - containerClip.Y);
        
        if (width > maxWidth)
        {
            width = maxWidth;
        }
        
        if (height > maxHeight)
        {
            height = maxHeight;
        }
        
        if (width <= 0 || height <= 0)
        {
            throw new Exception($"Invalid crop dimensions: {width}x{height}");
        }
        
        return new Clip
        {
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }
}

