using AF0E.Functions.DX.Infrastructure;
using Azure.Data.Tables;

namespace AF0E.Functions.DX.Activities;

public sealed class LoadSavedDataActivity
{
    public const string ActivityName = nameof(LoadSavedDataActivity);

    /*
    private readonly ILogger<LoadSavedDataActivity> _logger;

    public LoadSavedDataActivity(ILogger<LoadSavedDataActivity> logger)
    {
        _logger = logger;
    }
    */


#pragma warning disable CA1822 //Member 'Run' does not access instance data and can be marked as static
    [Function(ActivityName)]
    public async Task<List<DxInfo>> Run([ActivityTrigger] Tuple<DateTime, DateTime> dateRange)
#pragma warning restore CA1822
    {
        var svcClient = new TableServiceClient(Environment.GetEnvironmentVariable("AzureTableStorage"));
        var tblClient = svcClient.GetTableClient("DxInfo");

        await tblClient.CreateIfNotExistsAsync();

        var data = tblClient.Query<DxInfoDto>(
            $"PartitionKey ge '{dateRange.Item1:yyyyMM}' and PartitionKey le '{dateRange.Item2:yyyyMM}'")
            .Select(x => new DxInfo(x))
            .DistinctBy(x => x.CallSign)
            .ToList();

        return data;
    }
}
