using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using Microsoft.Playwright;

namespace BomLocalService.Services.Scraping.Steps.Navigation;

public class NavigateHomepageStep : BaseScrapingStep
{
    private readonly string _baseUrl;
    
    public override string Name => "NavigateHomepage";
    public override string[] Prerequisites => Array.Empty<string>();
    
    public NavigateHomepageStep(
        ILogger<NavigateHomepageStep> logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration)
        : base(logger, selectorService, debugService, configuration)
    {
        _baseUrl = configuration.GetValue<string>("Scraping:BaseUrl") ?? "https://www.bom.gov.au/";
    }
    
    public override bool CanExecute(ScrapingContext context)
    {
        return context.CurrentState == PageState.Initial;
    }
    
    public override async Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Step {Step}: Navigating to BOM homepage", Name);
            
            await context.Page.GotoAsync(_baseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30000
            });
            
            // Wait for search button to be ready
            var searchButtonReady = SelectorService.GetLocator(context.Page, Selectors.SearchButton);
            await searchButtonReady.WaitForAsync(new LocatorWaitForOptions 
            { 
                Timeout = Selectors.SearchButton.TimeoutMs, 
                State = WaitForSelectorState.Visible 
            });
            
            await SaveDebugAsync(context, 1, "homepage_loaded", cancellationToken);
            
            context.CurrentState = PageState.HomepageLoaded;
            return ScrapingStepResult.Successful();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Step {Step} failed", Name);
            await SaveErrorDebugAsync(context, $"Failed to navigate to homepage: {ex.Message}", cancellationToken);
            return ScrapingStepResult.Failed($"Failed to navigate to homepage: {ex.Message}");
        }
    }
}

