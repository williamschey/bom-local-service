using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using Microsoft.Playwright;

namespace BomLocalService.Services.Scraping.Steps.Map;

public class WaitForMapReadyStep : BaseScrapingStep
{
    private readonly int _tileRenderWaitMs;
    
    public override string Name => "WaitForMapReady";
    public override string[] Prerequisites => new[] { "ClickRadarLink" };
    
    public WaitForMapReadyStep(
        ILogger<WaitForMapReadyStep> logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration)
        : base(logger, selectorService, debugService, configuration)
    {
        _tileRenderWaitMs = configuration.GetValue<int>("Screenshot:TileRenderWaitMs", 5000);
    }
    
    public override bool CanExecute(ScrapingContext context)
    {
        return context.IsRadarPageLoaded;
    }
    
    public override async Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Step {Step}: Waiting for weather map page to load", Name);
            
            await context.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 15000 });
            
            Logger.LogInformation("Step {Step}: Waiting for map canvas element to render", Name);
            var mapCanvas = SelectorService.GetLocator(context.Page, Selectors.MapCanvas);
            await mapCanvas.WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
            
            await context.Page.WaitForFunctionAsync(
                JavaScriptTemplates.WaitForMapCanvas, 
                new PageWaitForFunctionOptions { Timeout = 15000 });
            
            Logger.LogInformation("Step {Step}: Map canvas is ready - waiting for map to render", Name);
            
            try
            {
                await context.Page.WaitForFunctionAsync(
                    JavaScriptTemplates.WaitForEsriView, 
                    new PageWaitForFunctionOptions { Timeout = 30000 });
                Logger.LogInformation("Step {Step}: Esri map view is ready", Name);
            }
            catch
            {
                Logger.LogInformation("Step {Step}: Esri view ready check timed out, continuing with fixed wait", Name);
            }
            
            await context.Page.WaitForTimeoutAsync(_tileRenderWaitMs);
            await SaveDebugAsync(context, 7, "weather_map_ready", cancellationToken);
            
            context.CurrentState = PageState.MapReady;
            return ScrapingStepResult.Successful();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Step {Step} failed", Name);
            await SaveErrorDebugAsync(context, $"Failed to wait for map ready: {ex.Message}", cancellationToken);
            return ScrapingStepResult.Failed($"Failed to wait for map ready: {ex.Message}");
        }
    }
}

