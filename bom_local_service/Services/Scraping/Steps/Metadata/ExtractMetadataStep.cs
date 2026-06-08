using System.Text.Json;
using BomLocalService.Models;
using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using BomLocalService.Utilities;

namespace BomLocalService.Services.Scraping.Steps.Metadata;

public class ExtractMetadataStep : BaseScrapingStep
{
    private readonly ITimeParsingService _timeParsingService;

    public override string Name => "ExtractMetadata";
    public override string[] Prerequisites => new[] { "PauseRadar" };

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

            var frameCount = CacheHelper.GetFrameCountForDataType(Configuration, CachedDataType.Radar);
            var defaults = Enumerable.Range(0, frameCount)
                .Select(i => (index: i, minutesAgo: 40 - i * 5))
                .ToList();

            try
            {
                var element = await context.Page.EvaluateAsync<JsonElement>(JavaScriptTemplates.ExtractFrameInfo);
                if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
                {
                    var byIndex = new Dictionary<int, int>();
                    foreach (var item in element.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object)
                            continue;

                        var idx = item.GetProperty("index").GetInt32();
                        if (item.TryGetProperty("minutesAgo", out var m) && m.ValueKind == JsonValueKind.Number)
                            byIndex[idx] = m.GetInt32();
                    }

                    if (byIndex.Count > 0)
                    {
                        context.FrameInfo = Enumerable.Range(0, frameCount)
                            .Select(i => (i, byIndex.TryGetValue(i, out var mm) ? mm : defaults[i].minutesAgo))
                            .ToList();
                        Logger.LogInformation("Step {Step}: Parsed frame offsets from segment aria-labels where available", Name);
                    }
                    else
                    {
                        context.FrameInfo = defaults;
                        Logger.LogInformation("Step {Step}: Segment times not parsed; using default ladder", Name);
                    }
                }
                else
                {
                    context.FrameInfo = defaults;
                    Logger.LogInformation("Step {Step}: No segment array from page; using default ladder", Name);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Step {Step}: Failed to extract frame info, using defaults", Name);
                context.FrameInfo = defaults;
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
