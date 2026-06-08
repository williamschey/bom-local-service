using BomLocalService.Models;
using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using BomLocalService.Utilities;
using Microsoft.Playwright;

namespace BomLocalService.Services.Scraping.Steps.Map;

/// <summary>
/// Aligns the radar scrubber to the oldest frame (historical frame 0) without clicking timeline segments.
/// The BOM UI now often exposes a single step-through control; segment pills and <c>bom-scrub-tl</c> are unreliable.
/// Strategy: while paused, read <see cref="IScrubDisplayTimestampParser"/> from the display label, step forward
/// through one full loop to find the minimum time, then step until that time shows again.
/// </summary>
public class ResetToFirstFrameStep : BaseScrapingStep
{
    private readonly IScrubDisplayTimestampParser _scrubDisplayTime;

    public override string Name => "ResetToFirstFrame";
    public override string[] Prerequisites => new[] { "ExtractMetadata" };

    public ResetToFirstFrameStep(
        ILogger<ResetToFirstFrameStep> logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration,
        IScrubDisplayTimestampParser scrubDisplayTime)
        : base(logger, selectorService, debugService, configuration)
    {
        _scrubDisplayTime = scrubDisplayTime;
    }

    public override bool CanExecute(ScrapingContext context)
    {
        return context.CurrentState >= PageState.SlideshowPaused
               && context.LastUpdatedInfo != null;
    }

    public override async Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Step {Step}: Aligning to oldest frame via step-through + display label (no segment clicks)", Name);

            var frameCount = CacheHelper.GetFrameCountForDataType(Configuration, CachedDataType.Radar);
            var elementWaitMs = Configuration.GetValue<int?>("Scraping:Timeouts:ElementWait") ?? 10000;

            var stepBtn = SelectorService.GetLocator(context.Page, Selectors.StepForwardButton);
            await stepBtn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = elementWaitMs });

            // Phase 1 — sample one full loop of display times while paused; oldest = frame 0 target
            var samples = new List<DateTime>();
            for (var i = 0; i < frameCount; i++)
            {
                var t = await _scrubDisplayTime.ReadUtcAsync(context.Page, SelectorService, context, cancellationToken);
                if (t.HasValue)
                    samples.Add(t.Value);

                if (i == frameCount - 1)
                    break;

                var beforeStep = t;
                await stepBtn.ClickAsync(new LocatorClickOptions { Force = true });
                if (beforeStep.HasValue)
                {
                    await _scrubDisplayTime.WaitForDisplayLabelChangeAsync(
                        context.Page, SelectorService, beforeStep.Value, context, maxWaitMs: 8000, cancellationToken: cancellationToken);
                }
                else
                    await context.Page.WaitForTimeoutAsync(800);
            }

            DateTime? frame0Utc = null;
            if (samples.Count > 0)
            {
                frame0Utc = samples.Min();
                Logger.LogInformation("Step {Step}: Oldest time in sampled loop (frame 0 target): {Utc:o} UTC", Name, frame0Utc);
            }
            else
            {
                Logger.LogWarning("Step {Step}: Could not read any display timestamps; tile timing may be wrong", Name);
            }

            // Phase 2 — step until the display matches the oldest sample
            if (frame0Utc.HasValue)
            {
                const double matchMinutes = 2.0;
                for (var attempt = 0; attempt < frameCount + 4; attempt++)
                {
                    var cur = await _scrubDisplayTime.ReadUtcAsync(context.Page, SelectorService, context, cancellationToken);
                    if (cur.HasValue && Math.Abs((cur.Value - frame0Utc.Value).TotalMinutes) <= matchMinutes)
                    {
                        Logger.LogInformation("Step {Step}: Display matches frame 0 target (≤{MatchMinutes:F0} min)", Name, matchMinutes);
                        break;
                    }

                    await stepBtn.ClickAsync(new LocatorClickOptions { Force = true });
                    if (cur.HasValue)
                    {
                        await _scrubDisplayTime.WaitForDisplayLabelChangeAsync(
                            context.Page, SelectorService, cur.Value, context, maxWaitMs: 8000, cancellationToken: cancellationToken);
                    }
                    else
                        await context.Page.WaitForTimeoutAsync(800);
                }
            }

            await SaveDebugAsync(context, 9, "frame_0_aligned", cancellationToken);

            // Best-effort only: segment-based checks are unreliable on the new spatial map layout
            try
            {
                var onSegmentZero = await context.Page.EvaluateAsync<bool>(JavaScriptTemplates.CheckActiveFrameSegment);
                if (onSegmentZero)
                    Logger.LogDebug("Step {Step}: Segment check reports frame 0 (informational)", Name);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Step {Step}: Segment position check skipped", Name);
            }

            await SaveDebugAsync(context, 10, "scrubber_frame0", cancellationToken);

            var tileRenderWaitMsConfig = Configuration.GetValue<int?>("Screenshot:TileRenderWaitMs");
            if (!tileRenderWaitMsConfig.HasValue)
            {
                throw new InvalidOperationException("Screenshot:TileRenderWaitMs configuration is required. Set it in appsettings.json or via SCREENSHOT__TILERENDERWAITMS environment variable.");
            }

            await context.Page.WaitForTimeoutAsync(tileRenderWaitMsConfig.Value);

            context.CurrentState = PageState.Frame0Selected;
            return ScrapingStepResult.Successful();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Step {Step} failed", Name);
            await SaveErrorDebugAsync(context, $"Failed to reset to first frame: {ex.Message}", cancellationToken);
            return ScrapingStepResult.Failed($"Failed to reset to first frame: {ex.Message}");
        }
    }
}
