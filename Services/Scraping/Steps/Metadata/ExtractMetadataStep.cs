using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;

namespace BomLocalService.Services.Scraping.Steps.Metadata;

public class ExtractMetadataStep : BaseScrapingStep
{
    private readonly ITimeParsingService _timeParsingService;
    
    public override string Name => "ExtractMetadata";
    public override string[] Prerequisites => new[] { "ResetToFirstFrame" };
    
    public ExtractMetadataStep(
        ILogger<ExtractMetadataStep> logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration,
        ITimeParsingService timeParsingService)
        : base(logger, selectorService, debugService, configuration)
    {
        _timeParsingService = timeParsingService;
    }
    
    public override bool CanExecute(ScrapingContext context)
    {
        return context.IsMapReady;
    }
    
    public override async Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Step {Step}: Extracting metadata and frame information", Name);
            
            var lastUpdatedInfo = await _timeParsingService.ExtractLastUpdatedInfoAsync(context.Page);
            context.LastUpdatedInfo = lastUpdatedInfo;
            
            // Extract frame info
            try
            {
                var frameInfo = await context.Page.EvaluateAsync<object[]>(JavaScriptTemplates.ExtractFrameInfo);
                var result = new List<(int index, int minutesAgo)>();
                for (int i = 0; i < 7; i++)
                {
                    var minutesAgo = 40 - (i * 5);
                    result.Add((i, minutesAgo));
                }
                context.FrameInfo = result;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Step {Step}: Failed to extract frame info, using defaults", Name);
                context.FrameInfo = Enumerable.Range(0, 7)
                    .Select(i => (i, 40 - (i * 5)))
                    .ToList();
            }
            
            return ScrapingStepResult.Successful();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Step {Step} failed", Name);
            await SaveErrorDebugAsync(context, $"Failed to extract metadata: {ex.Message}", cancellationToken);
            return ScrapingStepResult.Failed($"Failed to extract metadata: {ex.Message}");
        }
    }
}

