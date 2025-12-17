namespace BomLocalService.Services.Scraping;

public class WorkflowFactory : IWorkflowFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public WorkflowFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IWorkflow<TResponse> GetWorkflow<TResponse>(string name)
    {
        return name switch
        {
            "RadarScraping" => (IWorkflow<TResponse>)_serviceProvider.GetRequiredService<Workflows.RadarScrapingWorkflow>(),
            "TemperatureMap" => (IWorkflow<TResponse>)_serviceProvider.GetRequiredService<Workflows.TemperatureMapWorkflow>(),
            _ => throw new ArgumentException($"Unknown workflow: {name}")
        };
    }
}

