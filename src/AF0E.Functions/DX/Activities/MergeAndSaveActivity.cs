using AF0E.Functions.DX.Infrastructure;
using Azure.Data.Tables;

namespace AF0E.Functions.DX.Activities;

public sealed class MergeAndSaveActivity(ILogger<MergeAndSaveActivity> logger)
{
    public const string ActivityName = nameof(MergeAndSaveActivity);

    [Function(ActivityName)]
    public async Task Run([ActivityTrigger] Tuple<List<DxInfo>, List<DxInfo>, DateTime> results)
    {
        var merged = results.Item1;
        var newData = results.Item2;
        var minDate = results.Item3;

        Merge();
        await Save();
        return;

        void Merge()
        {
            foreach (var dxInfo in newData)
            {
                var existing = merged.SingleOrDefault(x => x.CallSign == dxInfo.CallSign);

                if (existing == null)
                {
                    merged.Add(dxInfo);
                    continue;
                }

                if (string.IsNullOrEmpty(existing.Name))
                    existing.Name = dxInfo.Name;
                if (string.IsNullOrEmpty(existing.DXCC))
                    existing.DXCC = dxInfo.DXCC;
                if (string.IsNullOrEmpty(existing.IOTA))
                    existing.IOTA = dxInfo.IOTA;
                if (string.IsNullOrEmpty(existing.Description))
                    existing.Description = dxInfo.Description;
                if (!existing.BeginDateSet)
                    existing.BeginDateSet = dxInfo.BeginDateSet;
                if (!existing.EndDateSet)
                    existing.EndDateSet = dxInfo.EndDateSet;

                foreach (var link in dxInfo.Links)
                {
                    if (!existing.Links.Contains(link))
                        existing.Links.Add(link);
                }

                if (existing.BeginDate == dxInfo.BeginDate && existing.EndDate == dxInfo.EndDate)
                    continue;

#if DEBUG
                logger.LogDxDifference(dxInfo.CallSign, existing.BeginDate, existing.EndDate, existing.Source, dxInfo.BeginDate, dxInfo.EndDate, dxInfo.Source);
#endif

                if (existing.BeginDate > dxInfo.BeginDate)
                    existing.BeginDate = dxInfo.BeginDate;

                if (existing.EndDate < dxInfo.EndDate)
                    existing.EndDate = dxInfo.EndDate;
            }
        }

        async Task Save()
        {
            var svcClient = new TableServiceClient(
                new Uri(Environment.GetEnvironmentVariable("StorageUrl")),
                new TableSharedKeyCredential(Environment.GetEnvironmentVariable("StorageAccName"), Environment.GetEnvironmentVariable("StorageKey")));

            var tblClient = svcClient.GetTableClient("DxInfo");

            foreach (var result in merged)
            {
                if (result.EndDate < minDate) // already ended
                    continue;

                var endDate = new DateOnly(result.EndDate.Year, result.EndDate.Month, 1);
                var iterator = result.BeginDate > minDate
                    ? new DateOnly(result.BeginDate.Year, result.BeginDate.Month, 1)
                    : new DateOnly(minDate.Year, minDate.Month, minDate.Day);

                while (iterator <= endDate)
                {
                    var entity = new DxInfoDto(iterator.ToString("yyyyMM"), result);
                    await tblClient.UpsertEntityAsync(entity);
                    iterator = iterator.AddMonths(1);
                }
            }
        }
    }
}
