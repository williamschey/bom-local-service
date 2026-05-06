using BomLocalService.Services.Interfaces.Registration;
using BomLocalService.Services.Scraping;
using Microsoft.Playwright;

namespace BomLocalService.Services.Interfaces;

/// <summary>
/// Parses the BOM scrubber time display label to UTC; shared by capture and reset steps.
/// </summary>
public interface IScrubDisplayTimestampParser : ISingletonService
{
    Task<DateTime?> ReadUtcAsync(
        IPage page,
        ISelectorService selectorService,
        ScrapingContext? context = null,
        CancellationToken cancellationToken = default);

    bool TryParseTimestamp(string timestampStr, string? timezoneAbbreviation, out DateTime timestampUtc);

    Task WaitForDisplayLabelChangeAsync(
        IPage page,
        ISelectorService selectorService,
        DateTime currentTimestamp,
        ScrapingContext? context = null,
        int maxWaitMs = 5000,
        CancellationToken cancellationToken = default);
}
