using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using Microsoft.Playwright;

namespace BomLocalService.Services.Scraping.Steps.Navigation;

public class ClickSearchButtonStep : BaseScrapingStep
{
    public override string Name => "ClickSearchButton";
    public override string[] Prerequisites => new[] { "NavigateHomepage" };
    
    public ClickSearchButtonStep(
        ILogger<ClickSearchButtonStep> logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration)
        : base(logger, selectorService, debugService, configuration)
    {
    }
    
    public override bool CanExecute(ScrapingContext context)
    {
        return context.IsHomepageLoaded;
    }
    
    public override async Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Step {Step}: Clicking 'Search for a location' button", Name);
            
            var searchButton = await SelectorService.FindElementAsync(context.Page, Selectors.SearchButton, cancellationToken);
            
            if (searchButton == null)
            {
                var errorMsg = Selectors.SearchButton.ErrorMessage ?? "Could not find 'Search for a location' button on BOM homepage.";
                await SaveErrorDebugAsync(context, errorMsg, cancellationToken);
                return ScrapingStepResult.Failed(errorMsg);
            }
            
            context.SearchButton = searchButton;
            await searchButton.ClickAsync();
            
            // Wait for search input to appear
            var searchInputReady = SelectorService.GetLocator(context.Page, Selectors.SearchInput);
            await searchInputReady.WaitForAsync(new LocatorWaitForOptions 
            { 
                Timeout = Selectors.SearchInput.TimeoutMs, 
                State = WaitForSelectorState.Visible 
            });
            
            await SaveDebugAsync(context, 2, "search_button_clicked", cancellationToken);
            
            context.CurrentState = PageState.SearchModalOpen;
            return ScrapingStepResult.Successful();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Step {Step} failed", Name);
            await SaveErrorDebugAsync(context, $"Failed to click search button: {ex.Message}", cancellationToken);
            return ScrapingStepResult.Failed($"Failed to click search button: {ex.Message}");
        }
    }
}

