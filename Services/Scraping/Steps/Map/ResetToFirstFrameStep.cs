using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using Microsoft.Playwright;

namespace BomLocalService.Services.Scraping.Steps.Map;

public class ResetToFirstFrameStep : BaseScrapingStep
{
    public override string Name => "ResetToFirstFrame";
    public override string[] Prerequisites => new[] { "PauseRadar" };
    
    public ResetToFirstFrameStep(
        ILogger<ResetToFirstFrameStep> logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration)
        : base(logger, selectorService, debugService, configuration)
    {
    }
    
    public override bool CanExecute(ScrapingContext context)
    {
        return context.CurrentState >= PageState.SlideshowPaused;
    }
    
    public override async Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Step {Step}: Resetting to first frame (frame 0)", Name);
            
            try
            {
                // Try JavaScript click first to avoid thumb interception
                var clicked = await context.Page.EvaluateAsync<bool>(@"() => {
                    const segment = document.querySelector('[data-testid=""bom-scrub-segment""][data-id=""0""]');
                    if (segment) {
                        segment.click();
                        return true;
                    }
                    return false;
                }");
                
                if (clicked)
                {
                    await context.Page.WaitForTimeoutAsync(500);
                    Logger.LogInformation("Step {Step}: Successfully clicked frame 0 segment via JavaScript", Name);
                }
                else
                {
                    // Fallback to locator click with force
                    var firstFrameSegment = context.Page.Locator("[data-testid='bom-scrub-segment'][data-id='0']").First;
                    await firstFrameSegment.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
                    await firstFrameSegment.ClickAsync(new LocatorClickOptions { Force = true });
                    await context.Page.WaitForTimeoutAsync(500);
                    Logger.LogInformation("Step {Step}: Successfully clicked frame 0 segment via force click", Name);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Step {Step}: Failed to click first frame segment, continuing anyway", Name);
            }
            
            await SaveDebugAsync(context, 9, "frame_0_selected", cancellationToken);
            
            // Verify scrubber is at position 0
            Logger.LogInformation("Step {Step}: Verifying scrubber is at position 0", Name);
            try
            {
                var activeSegment = await context.Page.EvaluateAsync<bool>(JavaScriptTemplates.CheckActiveFrameSegment);
                
                if (activeSegment)
                {
                    Logger.LogInformation("Step {Step}: Scrubber confirmed at position 0", Name);
                }
                else
                {
                    Logger.LogDebug("Step {Step}: Could not confirm scrubber position, but continuing", Name);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Step {Step}: Scrubber position verification failed, continuing anyway", Name);
            }
            
            await SaveDebugAsync(context, 10, "scrubber_at_position_0", cancellationToken);
            
            // Wait for frame 0 tiles to fully render
            var tileRenderWaitMsConfig = Configuration.GetValue<int?>("Screenshot:TileRenderWaitMs");
            if (!tileRenderWaitMsConfig.HasValue)
            {
                throw new InvalidOperationException("Screenshot:TileRenderWaitMs configuration is required. Set it in appsettings.json or via SCREENSHOT__TILERENDERWAITMS environment variable.");
            }
            var tileRenderWaitMs = tileRenderWaitMsConfig.Value;
            await context.Page.WaitForTimeoutAsync(tileRenderWaitMs);
            
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

