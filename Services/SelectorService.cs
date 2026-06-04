using BomLocalService.Models;
using BomLocalService.Services.Interfaces;
using Microsoft.Playwright;

namespace BomLocalService.Services;

public class SelectorService : ISelectorService
{
    private readonly ILogger<SelectorService> _logger;
    
    public SelectorService(ILogger<SelectorService> logger)
    {
        _logger = logger;
    }
    
    public async Task<ILocator?> FindElementAsync(IPage page, SelectorConfig config, CancellationToken cancellationToken = default)
    {
        foreach (var selector in config.Selectors)
        {
            try
            {
                var locator = page.Locator(selector).First;
                if (await locator.IsVisibleAsync())
                {
                    _logger.LogInformation("Found {Name} with selector: {Selector}", config.Name, selector);
                    return locator;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Selector {Selector} not found or not visible for {Name}", selector, config.Name);
                continue;
            }
        }
        
        if (config.Required)
        {
            var errorMsg = config.ErrorMessage ?? $"Required element {config.Name} not found with any selector";
            _logger.LogWarning("{Message}", errorMsg);
        }
        else
        {
            _logger.LogDebug("Optional element {Name} not found with any selector", config.Name);
        }
        
        return null;
    }
    
    public ILocator GetLocator(IPage page, SelectorConfig config)
    {
        if (config.Selectors.Length == 0)
        {
            throw new ArgumentException($"No selectors configured for {config.Name}");
        }

        // Match any configured selector (BOM occasionally renames test ids; list is tried in order)
        var combined = page.Locator(config.Selectors[0]).First;
        for (var i = 1; i < config.Selectors.Length; i++)
        {
            combined = combined.Or(page.Locator(config.Selectors[i]).First);
        }

        // Or() unions alternatives; BOM sometimes renders duplicates (e.g. responsive + hidden).
        // Pin to a single element so Playwright strict mode does not reject clicks.
        return combined.First;
    }
}

