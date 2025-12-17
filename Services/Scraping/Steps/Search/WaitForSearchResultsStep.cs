using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using Microsoft.Playwright;

namespace BomLocalService.Services.Scraping.Steps.Search;

public class WaitForSearchResultsStep : BaseScrapingStep
{
    public override string Name => "WaitForSearchResults";
    public override string[] Prerequisites => new[] { "FillSearchInput" };
    
    public WaitForSearchResultsStep(
        ILogger<WaitForSearchResultsStep> logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration)
        : base(logger, selectorService, debugService, configuration)
    {
    }
    
    public override bool CanExecute(ScrapingContext context)
    {
        return context.IsSearchModalOpen;
    }
    
    public override async Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Step {Step}: Waiting for autocomplete suggestions", Name);
            
            await context.Page.WaitForFunctionAsync(
                JavaScriptTemplates.WaitForSearchResults, 
                new PageWaitForFunctionOptions { Timeout = 10000 });
            
            context.CurrentState = PageState.SearchResultsVisible;
            return ScrapingStepResult.Successful();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Step {Step} failed", Name);
            await SaveErrorDebugAsync(context, $"Failed to wait for search results: {ex.Message}", cancellationToken);
            return ScrapingStepResult.Failed($"Failed to wait for search results: {ex.Message}");
        }
    }
}

