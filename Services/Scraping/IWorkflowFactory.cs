namespace BomLocalService.Services.Scraping;

/// <summary>
/// Factory for creating workflows
/// </summary>
public interface IWorkflowFactory
{
    IWorkflow<TResponse> GetWorkflow<TResponse>(string name);
}

