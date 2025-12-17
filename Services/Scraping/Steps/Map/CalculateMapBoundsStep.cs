using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using Microsoft.Playwright;

namespace BomLocalService.Services.Scraping.Steps.Map;

public class CalculateMapBoundsStep : BaseScrapingStep
{
    public override string Name => "CalculateMapBounds";
    public override string[] Prerequisites => new[] { "ResetToFirstFrame" };
    
    public CalculateMapBoundsStep(
        ILogger<CalculateMapBoundsStep> logger,
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
            Logger.LogInformation("Step {Step}: Preparing map container for screenshot", Name);
            
            var mapContainer = SelectorService.GetLocator(context.Page, Selectors.MapContainer);
            await mapContainer.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            
            await context.Page.WaitForFunctionAsync(
                JavaScriptTemplates.WaitForMapContainer, 
                new PageWaitForFunctionOptions { Timeout = 10000 });
            
            var boundingBox = await mapContainer.BoundingBoxAsync();
            if (boundingBox == null || boundingBox.Width <= 0 || boundingBox.Height <= 0)
            {
                var errorMsg = $"Map container has invalid bounds: {boundingBox?.Width ?? 0}x{boundingBox?.Height ?? 0}";
                Logger.LogError("Step {Step}: {Error}", Name, errorMsg);
                await SaveErrorDebugAsync(context, errorMsg, cancellationToken);
                return ScrapingStepResult.Failed(errorMsg);
            }
            
            var containerClip = new Clip
            {
                X = boundingBox.X,
                Y = boundingBox.Y,
                Width = boundingBox.Width,
                Height = boundingBox.Height
            };
            
            context.MapContainer = mapContainer;
            context.MapBoundingBox = containerClip;
            
            return ScrapingStepResult.Successful();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Step {Step} failed", Name);
            await SaveErrorDebugAsync(context, $"Failed to calculate map bounds: {ex.Message}", cancellationToken);
            return ScrapingStepResult.Failed($"Failed to calculate map bounds: {ex.Message}");
        }
    }
}

