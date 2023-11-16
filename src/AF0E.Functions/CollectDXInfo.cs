using System.Net;

namespace AF0E.Functions;

using Microsoft.DurableTask.Client;

public class CollectDXInfo
{
    private readonly ILogger<CollectDXInfo> _logger;

    public const string OrchestratorFunctionName = nameof(CollectDXInfo) + "Orchestrator";
    public const string HttpStarterFunctionName = nameof(CollectDXInfo) + "HttpStarter";
    public const string TimerStarterFunctionName = nameof(CollectDXInfo) + "TimerStarter";
    
    public CollectDXInfo(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CollectDXInfo>();
    }

    //[Function(TimerStarterFunctionName)]
    public async Task TimerStarterAsync([TimerTrigger("0 */1 * * * *")] MyInfo myTimer, [DurableClient] DurableTaskClient starter) // 0 0 0 * * * - at 12am
    {
        _logger.LogInformation("!!!   C# Timer trigger function executed at: {Now}", DateTime.Now);
        _logger.LogInformation("!!!   Next timer schedule at: {ScheduleStatusNext}", myTimer.ScheduleStatus.Next);
        
        await starter.ScheduleNewOrchestrationInstanceAsync(OrchestratorFunctionName);
    }
        
    [Function(HttpStarterFunctionName)]
    public async Task<HttpResponseData> HttpStarterAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, [DurableClient] DurableTaskClient starter) 
    {
        _logger.LogInformation("!!!   C# HTTP trigger function processed a request at: {Now}", DateTime.Now);

        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(OrchestratorFunctionName);
        return starter.CreateCheckStatusResponse(req, instanceId);
    }        
    
    [Function(OrchestratorFunctionName)]
    public async Task OrchestratorAsync([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        _logger.LogInformation("!!!   Orchestrator executed");
        await context.CallActivityAsync<string>(nameof(ProcessSource));
        /*
        var input = context.GetInput<SentimentUserInput>();

        // Get a list of N work items to process in parallel.
        var tasks = new List<Task<SentimentResult>>();
        for (var userId = 1; userId <= input.NumberOfUsers; userId++)
        {
            tasks.Add(context.CallActivityAsync<SentimentResult>(nameof(CalculateUserSentiment), userId));
        }

        await Task.WhenAll(tasks);

        var results = tasks.Select(x => x.Result);
        await context.CallActivityAsync(nameof(PrintReport), results);
    */
    }
    
    [Function(nameof(ProcessSource))]
    public async Task<string> ProcessSource([ActivityTrigger] object? param = null)
    {
        _logger.LogInformation("!!!   DX process executed");
        await Task.Delay(1);
        return "DX info";
    }    
}

public class MyInfo
{
    public MySchedule Schedule { get; set; }
        
    public MyScheduleStatus ScheduleStatus { get; set; }

    public bool IsPastDue { get; set; }
}

public class MyScheduleStatus
{
    public DateTime Last { get; set; }

    public DateTime Next { get; set; }

    public DateTime LastUpdated { get; set; }
}
    
public class MySchedule
{
    public bool AdjustForDST { get; set; }
}
