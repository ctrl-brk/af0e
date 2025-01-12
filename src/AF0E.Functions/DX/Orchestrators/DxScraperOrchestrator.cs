using AF0E.Functions.DX.Activities;
using AF0E.Functions.DX.Infrastructure;
#pragma warning disable CA1062

namespace AF0E.Functions.DX.Orchestrators;

public sealed class DxScraperOrchestrator
{
    public const string OrchestratorName = nameof(DxScraperOrchestrator);

    [Function(OrchestratorName)]
    public static async Task Orchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var log = context.CreateReplaySafeLogger<DxScraperOrchestrator>();

        var activities = new List<Task<ScrapeActivityResult>>
        {
            context.CallActivityAsync<ScrapeActivityResult>(DxInfoDotNetActivity.ActivityName),
            context.CallActivityAsync<ScrapeActivityResult>(DxMapsDotComActivity.ActivityName),
            context.CallActivityAsync<ScrapeActivityResult>(Va3RjActivity.ActivityName),
        };

        await Task.WhenAll(activities);

        var dxData = activities
            .Where(x => x is { IsCompletedSuccessfully: true, Result.IsSuccess: true })
            .Select(x => x.Result.Result).SelectMany(x => x!).ToList();

        if (dxData.Count == 0)
        {
            log.LogNoDx();
            return;
        }

        var dt = context.CurrentUtcDateTime;
        var minDate = new DateTime(dt.Year, dt.Month, dt.Day);
        var maxReceivedDate = dxData.Max(x => x.EndDate);

        var savedData = await context.CallActivityAsync<List<DxInfo>>(LoadSavedDataActivity.ActivityName, new Tuple<DateTime, DateTime>(minDate, maxReceivedDate));

        await context.CallActivityAsync(MergeAndSaveActivity.ActivityName, new Tuple<List<DxInfo>, List<DxInfo>, DateTime>(savedData, dxData, minDate));
    }
}
