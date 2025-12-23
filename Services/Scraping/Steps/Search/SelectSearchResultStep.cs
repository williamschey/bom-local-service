using BomLocalService.Services.Interfaces;
using BomLocalService.Services.Scraping;
using BomLocalService.Utilities;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace BomLocalService.Services.Scraping.Steps.Search;

public class SelectSearchResultStep : BaseScrapingStep
{
    public override string Name => "SelectSearchResult";
    public override string[] Prerequisites => new[] { "WaitForSearchResults" };
    
    public SelectSearchResultStep(
        ILogger<SelectSearchResultStep> logger,
        ISelectorService selectorService,
        IDebugService debugService,
        IConfiguration configuration)
        : base(logger, selectorService, debugService, configuration)
    {
    }
    
    public override bool CanExecute(ScrapingContext context)
    {
        return context.CurrentState >= PageState.SearchResultsVisible;
    }
    
    public override async Task<ScrapingStepResult> ExecuteAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Step {Step}: Looking for matching search result for {Suburb}, {State}", Name, context.Suburb, context.State);
            
            var suburbLower = context.Suburb.ToLower().Trim();
            var stateLower = context.State.ToLower().Trim();
            
            // Get the actual count from the summary element
            var resultsTitle = SelectorService.GetLocator(context.Page, Selectors.ResultsTitle);
            var summaryText = await resultsTitle.TextContentAsync();
            int? actualCount = null;
            
            if (!string.IsNullOrEmpty(summaryText))
            {
                var countMatch = Regex.Match(summaryText, TextPatterns.ResultsCountPattern, RegexOptions.IgnoreCase);
                if (countMatch.Success && countMatch.Groups.Count >= 3 && countMatch.Groups[2].Success)
                {
                    if (int.TryParse(countMatch.Groups[2].Value, out var total))
                    {
                        actualCount = total;
                    }
                }
            }
            
            // Fetch all result data
            List<(string name, string desc, string fullText)> results = new();
            try
            {
                var resultData = await context.Page.EvaluateAsync<string[][]>(JavaScriptTemplates.ExtractSearchResults);
                
                results = resultData.Select(arr => (
                    name: arr.Length > 0 ? arr[0] : "",
                    desc: arr.Length > 1 ? arr[1] : "",
                    fullText: arr.Length > 2 ? arr[2] : ""
                )).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to extract structured result data, falling back to text content");
                var resultTexts = await context.Page.EvaluateAsync<string[]>(JavaScriptTemplates.ExtractSearchResultsFallback);
                results = resultTexts.Select(text => (name: "", desc: "", fullText: text)).ToList();
            }
            
            if (actualCount.HasValue)
            {
                Logger.LogInformation("Found {Count} location results (summary: {Summary})", actualCount.Value, summaryText?.Trim());
                if (results.Count > actualCount.Value)
                {
                    results = results.Take(actualCount.Value).ToList();
                }
            }
            else
            {
                Logger.LogInformation("Found {Count} search results", results.Count);
            }
            
            int? matchingIndex = null;
            int bestMatchScore = -1;
            
            for (int i = 0; i < results.Count; i++)
            {
                var (name, desc, fullText) = results[i];
                var nameLower = name.ToLower().Trim();
                var descLower = desc.ToLower().Trim();
                var fullTextLower = fullText.ToLower();
                
                Logger.LogInformation("Checking result {Index}: Name='{Name}', Desc='{Desc}', FullText='{FullText}'", 
                    i, name, desc, fullText.Length > 100 ? fullText.Substring(0, 100) + "..." : fullText);
                
                var matchesSuburb = false;
                var matchScore = 0;
                
                if (!string.IsNullOrEmpty(name))
                {
                    if (nameLower == suburbLower)
                    {
                        matchesSuburb = true;
                        matchScore = 100;
                    }
                    else if (nameLower.StartsWith(suburbLower + " ") || nameLower.StartsWith(suburbLower + "("))
                    {
                        matchesSuburb = true;
                        matchScore = 80;
                    }
                    else if (nameLower.Contains("(" + suburbLower + ")") || nameLower.Contains("(" + suburbLower + " "))
                    {
                        matchesSuburb = true;
                        matchScore = 60;
                    }
                    else if (nameLower.Contains(suburbLower))
                    {
                        matchesSuburb = true;
                        matchScore = 40;
                    }
                }
                else
                {
                    if (fullTextLower.Contains(suburbLower))
                    {
                        matchesSuburb = true;
                        matchScore = 20;
                    }
                }
                
                var matchesState = false;
                if (!string.IsNullOrEmpty(desc))
                {
                    matchesState = StateAbbreviationHelper.MatchesState(descLower, stateLower);
                }
                if (!matchesState)
                {
                    matchesState = StateAbbreviationHelper.MatchesState(fullTextLower, stateLower);
                }
                
                Logger.LogInformation("Result {Index}: matchesSuburb={MatchesSuburb} (score={Score}), matchesState={MatchesState}", 
                    i, matchesSuburb, matchScore, matchesState);
                
                if (matchesSuburb && matchesState && matchScore > bestMatchScore)
                {
                    matchingIndex = i;
                    bestMatchScore = matchScore;
                    Logger.LogInformation("New best match found: {Name} - {Desc} (score: {Score})", name, desc, matchScore);
                }
            }
            
            if (matchingIndex.HasValue)
            {
                Logger.LogInformation("Using best matching result at index {Index} with score {Score}", matchingIndex.Value, bestMatchScore);
            }
            else
            {
                Logger.LogInformation("No exact match found, using first result");
            }
            
            context.SearchResults = results;
            context.SelectedResultIndex = matchingIndex;
            
            // Click the matching result
            var resultsList = context.Page.Locator("ul[aria-labelledby='location-results-title']");
            var allResults = resultsList.Locator("li.bom-linklist__item[role='listitem']");
            var resultToClick = matchingIndex.HasValue ? allResults.Nth(matchingIndex.Value) : allResults.First;
            
            await resultToClick.ClickAsync();
            await SaveDebugAsync(context, 4, "search_result_selected", cancellationToken);
            
            // Wait for forecast page to load
            await context.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 15000 });
            var dynamicContentWaitMsConfig = Configuration.GetValue<int?>("Screenshot:DynamicContentWaitMs");
            if (!dynamicContentWaitMsConfig.HasValue)
            {
                throw new InvalidOperationException("Screenshot:DynamicContentWaitMs configuration is required. Set it in appsettings.json or via SCREENSHOT__DYNAMICCONTENTWAITMS environment variable.");
            }
            var dynamicContentWaitMs = dynamicContentWaitMsConfig.Value;
            await context.Page.WaitForTimeoutAsync(dynamicContentWaitMs);
            await SaveDebugAsync(context, 5, "forecast_page_loaded", cancellationToken);
            
            context.CurrentState = PageState.ForecastPageLoaded;
            return ScrapingStepResult.Successful();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Step {Step} failed", Name);
            await SaveErrorDebugAsync(context, $"Failed to select search result: {ex.Message}", cancellationToken);
            return ScrapingStepResult.Failed($"Failed to select search result: {ex.Message}");
        }
    }
}

