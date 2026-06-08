using BomLocalService.Models;
using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using Microsoft.Playwright;

namespace BomLocalService.Services;

public class ScrapingService : IScrapingService
{
    private readonly ILogger<ScrapingService> _logger;
    private readonly IWorkflowFactory _workflowFactory;
    
    public ScrapingService(
        ILogger<ScrapingService> logger,
        IWorkflowFactory workflowFactory)
    {
        _logger = logger;
        _workflowFactory = workflowFactory;
    }
    
    /// <summary>
    /// Scrapes the BOM website to get a radar screenshot for a location
    /// </summary>
    public async Task<RadarResponse> ScrapeRadarScreenshotAsync(
        string suburb,
        string state,
        string cacheFolderPath,
        string debugFolder,
        IPage page,
        List<(string type, string text, DateTime timestamp)> consoleMessages,
        List<(string method, string url, int? status, string resourceType, DateTime timestamp)> networkRequests,
        CancellationToken cancellationToken = default)
    {
        var context = new ScrapingContext
        {
            Page = page,
            Suburb = suburb,
            State = state,
            CacheFolderPath = cacheFolderPath,
            DebugFolder = debugFolder,
            ConsoleMessages = consoleMessages,
            NetworkRequests = networkRequests
        };
        
        // Get the appropriate workflow (currently only RadarScraping is implemented)
        var workflow = _workflowFactory.GetWorkflow<RadarResponse>("RadarScraping");
        
        _logger.LogInformation("Executing {Workflow} workflow for {Suburb}, {State}", workflow.Name, suburb, state);
        
        return await workflow.ExecuteAsync(context, cancellationToken);
    }
}
