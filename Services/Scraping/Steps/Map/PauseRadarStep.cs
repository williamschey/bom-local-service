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
            
            var buttonLabel = await playPauseButton.Locator(Selectors.PlayPauseLabel.Selectors[0]).TextContentAsync();
            if (buttonLabel?.Trim().Equals(TextPatterns.ExpectedTexts["PauseButtonLabel"], StringComparison.OrdinalIgnoreCase) == true)
            {
                Logger.LogInformation("Step {Step}: Radar is playing, pausing it", Name);
                await playPauseButton.ClickAsync();
                await context.Page.WaitForTimeoutAsync(300);
                
                buttonLabel = await playPauseButton.Locator(Selectors.PlayPauseLabel.Selectors[0]).TextContentAsync();
                if (buttonLabel?.Trim().Equals(TextPatterns.ExpectedTexts["PlayButtonLabel"], StringComparison.OrdinalIgnoreCase) != true)
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

