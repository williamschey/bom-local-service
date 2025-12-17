using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using Microsoft.Playwright;

namespace BomLocalService.Services.Scraping.Steps.Navigation;

public class ClickRadarLinkStep : BaseScrapingStep
{
    public override string Name => "ClickRadarLink";
    public override string[] Prerequisites => new[] { "SelectSearchResult" };
    
    public ClickRadarLinkStep(
        ILogger<ClickRadarLinkStep> logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration)
        : base(logger, selectorService, debugService, configuration)
    {
    }
    
    public override bool CanExecute(ScrapingContext context)
    {
        return context.IsForecastPageLoaded;
    }
    
    public override async Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Step {Step}: Looking for 'Rain radar and weather map' link", Name);
            
            var radarLink = await SelectorService.FindElementAsync(context.Page, Selectors.RadarLink, cancellationToken);
            
            if (radarLink == null)
            {
                var errorMsg = Selectors.RadarLink.ErrorMessage ?? $"Could not find 'Rain radar and weather map' link for {context.Suburb}, {context.State}";
                await SaveErrorDebugAsync(context, errorMsg, cancellationToken);
                return ScrapingStepResult.Failed(errorMsg);
            }
            
            context.RadarLink = radarLink;
            await radarLink.ClickAsync();
            await SaveDebugAsync(context, 6, "radar_link_clicked", cancellationToken);
            
            context.CurrentState = PageState.RadarPageLoaded;
            return ScrapingStepResult.Successful();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Step {Step} failed", Name);
            await SaveErrorDebugAsync(context, $"Failed to click radar link: {ex.Message}", cancellationToken);
            return ScrapingStepResult.Failed($"Failed to click radar link: {ex.Message}");
        }
    }
}

