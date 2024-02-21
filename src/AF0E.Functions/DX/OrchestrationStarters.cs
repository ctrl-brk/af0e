using Microsoft.DurableTask.Client;
using AF0E.Functions.DX.Orchestrators;

#pragma warning disable CA1062 // null public methods arguments

namespace AF0E.Functions.DX;

public sealed class OrchestrationStarters
{
    /*
    private readonly ILogger<OrchestrationStarters> _logger;
    
    public OrchestrationStarters(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OrchestrationStarters>();
    }
    */
    
#pragma warning disable IDE0060
    // ReSharper disable once UnusedParameter.Global
    [Function(nameof(DxScraperTimerStarter))]
    public async Task DxScraperTimerStarter([TimerTrigger("%ScheduleStarterConfig%")] TimerInfo timerInfo, [DurableClient] DurableTaskClient starter) // https://crontab.guru/
#pragma warning restore IDE0060
    {
        if (Environment.GetEnvironmentVariable("UseSchedule") != "true")
            return;

        await starter.ScheduleNewOrchestrationInstanceAsync(DxScraperOrchestrator.OrchestratorName);
    }
        
    [Function(nameof(DxScraperHttpStarter))]
    public async Task<HttpResponseData> DxScraperHttpStarter([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, [DurableClient] DurableTaskClient starter) 
    {
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(DxScraperOrchestrator.OrchestratorName);
        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }        
}

#region TimerInfo
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
public class TimerInfo
{
    public TimerSchedule? Schedule { get; set; }
    public ScheduleStatus? ScheduleStatus { get; set; }
    public bool IsPastDue { get; set; }
}

public class TimerSchedule
{
    public bool AdjustForDst { get; set; }
}

public class ScheduleStatus
{
    public DateTime Last { get; set; }
    public DateTime Next { get; set; }
    public DateTime LastUpdated { get; set; }
}
#endregion
