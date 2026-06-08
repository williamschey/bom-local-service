using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using Microsoft.Playwright;

namespace BomLocalService.Services.Scraping.Steps.Map;

public class PauseRadarStep : BaseScrapingStep
{
    public override string Name => "PauseRadar";
    public override string[] Prerequisites => new[] { "WaitForMapReady" };
    
    public PauseRadarStep(
        ILogger<PauseRadarStep> logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration)
        : base(logger, selectorService, debugService, configuration)
    {
    }
    
    public override bool CanExecute(ScrapingContext context)
    {
        return context.IsMapReady;
    }
    
    public override async Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Step {Step}: Checking if radar loop is paused", Name);
            
            var playPauseButton = SelectorService.GetLocator(context.Page, Selectors.PlayPauseButton);
            await playPauseButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });

            // New spatial map often uses one step-through control; label may be on the button or in .bom-scrub-action__label
            string? buttonLabel = null;
            try
            {
                var labelLoc = playPauseButton.Locator(Selectors.PlayPauseLabel.Selectors[0]).First;
                if (await labelLoc.CountAsync() > 0)
                    buttonLabel = (await labelLoc.TextContentAsync())?.Trim();
            }
            catch
            {
                /* use inner text */
            }

            buttonLabel ??= (await playPauseButton.InnerTextAsync())?.Trim();

            if (buttonLabel?.Equals(TextPatterns.ExpectedTexts["PauseButtonLabel"], StringComparison.OrdinalIgnoreCase) == true)
            {
                Logger.LogInformation("Step {Step}: Radar is playing, pausing it", Name);
                await playPauseButton.ClickAsync();
                await context.Page.WaitForTimeoutAsync(300);

                // Clear so post-click read always runs; otherwise stale "Pause" skips the ??= InnerText fallback when the label child is missing
                buttonLabel = null;
                try
                {
                    var labelLoc = playPauseButton.Locator(Selectors.PlayPauseLabel.Selectors[0]).First;
                    if (await labelLoc.CountAsync() > 0)
                        buttonLabel = (await labelLoc.TextContentAsync())?.Trim();
                }
                catch
                {
                    buttonLabel = null;
                }

                buttonLabel ??= (await playPauseButton.InnerTextAsync())?.Trim();
                if (buttonLabel?.Equals(TextPatterns.ExpectedTexts["PlayButtonLabel"], StringComparison.OrdinalIgnoreCase) != true)
                {
                    Logger.LogWarning("Step {Step}: Radar may not be paused after click, continuing anyway", Name);
                }
            }
            else
            {
                Logger.LogInformation("Step {Step}: Radar is already paused", Name);
            }
            
            await SaveDebugAsync(context, 8, "radar_paused", cancellationToken);
            
            context.CurrentState = PageState.SlideshowPaused;
            return ScrapingStepResult.Successful();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Step {Step} failed", Name);
            await SaveErrorDebugAsync(context, $"Failed to pause radar: {ex.Message}", cancellationToken);
            return ScrapingStepResult.Failed($"Failed to pause radar: {ex.Message}");
        }
    }
}

