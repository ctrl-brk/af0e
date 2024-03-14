using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HrdDxFilter;

public class HostedService(ILogger<HostedService> logger, IHostApplicationLifetime appLifeTime, IOptions<AppSettings> settings) : IHostedService
{
    private Task? _task;
    private CancellationTokenSource? _cts;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogStarted();

        // Create a linked token, so we can trigger cancellation outside of this token's cancellation
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _task = GetDxScheduleAsync(_cts.Token);

        // If the task is completed then return it, otherwise it's running
        return _task.IsCompleted ? _task : Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task GetDxScheduleAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();

        try
        {
            var response = await client.GetAsync(settings.Value.DxApiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDxApiError(response.StatusCode, response.ReasonPhrase, response.RequestMessage);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
#pragma warning disable CA1869
                var results = JsonSerializer.Deserialize<IEnumerable<DxInfo>>(body, new JsonSerializerOptions{PropertyNameCaseInsensitive = true})!.ToArray();
#pragma warning restore CA1869
                var sortedResults = MassageResults(results);
                UpdateHrdFilterAsync(sortedResults);
            }
        }
        catch (Exception e)
        {
            logger.LogException(e);
        }

        appLifeTime.StopApplication();
    }

    private static DxInfo[] MassageResults(IEnumerable<DxInfo> results)
    {
        var sorted = results.OrderBy(x => x.CallSign).ToArray();

        foreach (var info in sorted.Where(x => x.CallSign.EndsWith(" PREFIX", StringComparison.OrdinalIgnoreCase)))
        {
            var arr = info.CallSign.Split(' ');
            info.CallSign = $"{arr[0]}.*";
        }

        return sorted;
    }

    private void UpdateHrdFilterAsync(IReadOnlyCollection<DxInfo> results)
    {
        if (results.Count == 0)
        {
            logger.LogNoDx();
            return;
        }

        var dxValue = string.Join('|', results.Select(x => x.CallSign));

        var filterTitle = settings.Value.HrdDxFilterTitle;

        if (string.IsNullOrEmpty(filterTitle))
            filterTitle = "DX";

        var filterFilePath = GetFiltersFilePath();

        var doc = XDocument.Load(filterFilePath);
       
        var dxFilter = doc.Descendants("Filter")
            .FirstOrDefault(x => (string)x.Attribute("Title")! == filterTitle);

        if (dxFilter is not null)
        {
            dxFilter.SetAttributeValue("DXCall", dxValue);
        }
        else
        {
            var newFilter = new XElement("Filter",
                new XAttribute("Title", "DX"),
                new XAttribute("DXCall", dxValue),
                new XAttribute("Filter", ""),
                new XAttribute("FreqMax", "0ll"),
                new XAttribute("FreqMin", "0ll"),
                new XAttribute("AndOr", "0"),
                new XAttribute("Max", "100"));
            
            doc.Element("HRD")!.Add(newFilter);
        }

        // Save the changes back to the file
        doc.Save(filterFilePath);
        logger.LogDxFilterValue(dxValue);
    }

    private string GetFiltersFilePath()
    {
        var path = settings.Value.HrdFiltersFile;

        if (string.IsNullOrEmpty(path))
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"HRDLLC\HRD Logbook\DXClusterFilters.xml");

        if (!File.Exists(path))
            throw new ApplicationException($"File {path} not found. Try to set HrdFiltersFile in the appsettings.json");

        return path;
    }
}
