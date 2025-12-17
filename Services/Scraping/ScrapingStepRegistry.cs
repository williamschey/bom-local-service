namespace BomLocalService.Services.Scraping;

public class ScrapingStepRegistry : IScrapingStepRegistry
{
    private readonly Dictionary<string, IScrapingStep> _steps = new();
    private readonly ILogger<ScrapingStepRegistry> _logger;
    
    public ScrapingStepRegistry(ILogger<ScrapingStepRegistry> logger)
    {
        _logger = logger;
    }
    
    public void RegisterStep(IScrapingStep step)
    {
        if (_steps.ContainsKey(step.Name))
        {
            _logger.LogWarning("Step {Name} is already registered, overwriting", step.Name);
        }
        _steps[step.Name] = step;
        _logger.LogDebug("Registered step: {Name}", step.Name);
    }
    
    public IScrapingStep? GetStep(string name)
    {
        return _steps.TryGetValue(name, out var step) ? step : null;
    }
    
    public IEnumerable<IScrapingStep> GetAllSteps()
    {
        return _steps.Values;
    }
}

