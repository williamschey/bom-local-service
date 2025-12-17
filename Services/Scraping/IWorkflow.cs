namespace BomLocalService.Services.Scraping;

/// <summary>
/// Interface for a scraping workflow
/// </summary>
/// <typeparam name="TResponse">The response type returned by this workflow</typeparam>
public interface IWorkflow<TResponse>
{
    string Name { get; }
    string Description { get; }
    string[] StepNames { get; } // Fixed order - cannot be changed
    Task<TResponse> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken);
}

