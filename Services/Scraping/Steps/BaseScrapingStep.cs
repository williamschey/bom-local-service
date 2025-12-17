using BomLocalService.Models;
using BomLocalService.Services.Interfaces;
using Microsoft.Playwright;

namespace BomLocalService.Services.Scraping.Steps;

/// <summary>
/// Base class for scraping steps with common functionality
/// </summary>
public abstract class BaseScrapingStep : IScrapingStep
{
    protected readonly ILogger Logger;
    protected readonly ISelectorService SelectorService;
    protected readonly IDebugService DebugService;
    protected readonly IConfiguration Configuration;
    protected readonly ScrapingSelectorsConfig Selectors;
    protected readonly JavaScriptTemplatesConfig JavaScriptTemplates;
    protected readonly TextPatternsConfig TextPatterns;
    
    protected BaseScrapingStep(
        ILogger logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration)
    {
        Logger = logger;
        SelectorService = selectorService;
        DebugService = debugService;
        Configuration = configuration;
        
        Selectors = configuration.GetSection("Scraping:Selectors").Get<ScrapingSelectorsConfig>() ?? new();
        JavaScriptTemplates = configuration.GetSection("Scraping:JavaScriptTemplates").Get<JavaScriptTemplatesConfig>() ?? new();
        TextPatterns = configuration.GetSection("Scraping:TextPatterns").Get<TextPatternsConfig>() ?? new();
    }
    
    public abstract string Name { get; }
    public abstract string[] Prerequisites { get; }
    public abstract bool CanExecute(ScrapingContext context);
    public abstract Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken);
    
    protected async Task SaveDebugAsync(ScrapingContext context, int stepNumber, string stepName, CancellationToken cancellationToken)
    {
        await DebugService.SaveStepDebugAsync(
            context.DebugFolder, 
            stepNumber, 
            stepName, 
            context.Page, 
            context.ConsoleMessages, 
            context.NetworkRequests, 
            cancellationToken);
    }
    
    protected async Task SaveErrorDebugAsync(ScrapingContext context, string errorMessage, CancellationToken cancellationToken)
    {
        await DebugService.SaveErrorDebugAsync(
            context.DebugFolder, 
            errorMessage, 
            context.Page, 
            context.ConsoleMessages, 
            context.NetworkRequests, 
            cancellationToken);
    }
}

