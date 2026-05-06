using System.Data;
using System.Data.Common;
using System.Text.Json;
using AF0E.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PotaParksImport;

public sealed class HostedService(ILogger<HostedService> logger, IHostApplicationLifetime appLifetime, IOptions<AppSettings> settings) : IHostedService
{
    private readonly record struct ImportSummary(int Inserted, int Updated, int Deactivated);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly string[] _args = Environment.GetCommandLineArgs();
    private Task? _task;
    private CancellationTokenSource? _cts;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogStarted();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _task = ImportAsync(_cts.Token);
        return _task.IsCompleted ? _task : Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ImportAsync(CancellationToken ct)
    {
        try
        {
            var dryRun = settings.Value.DryRun || _args.Any(x => x.Equals("--dry-run", StringComparison.OrdinalIgnoreCase));

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(settings.Value.HttpTimeoutSeconds) };

            logger.LogDownloading(settings.Value.ParksUrl);
            var response = await httpClient.GetAsync(settings.Value.ParksUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogHttpError(response.StatusCode);
                return;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var parks = JsonSerializer.Deserialize<List<PotaParkImportRow>>(body, JsonOptions);
            if (parks is null)
            {
                logger.LogJsonError(settings.Value.ParksUrl);
                return;
            }

            logger.LogDownloadedCount(parks.Count);
            if (parks.Count == 0)
            {
                logger.LogNoRows();
            }

            if (dryRun)
            {
                logger.LogDryRunEnabled();
                logger.LogDryRunCompleted(parks.Count);
                return;
            }

            await using var dbContext = new HrdDbContext(settings.Value.ConnectionString);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            logger.LogTruncating();
            await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE [dbo].[PotaParksImport]", ct);

            if (parks.Count > 0)
            {
                logger.LogInserting();
                await InsertRowsAsync(dbContext, parks, ct);
            }

            logger.LogRunningStoredProc();
            var summary = await ExecuteImportProcAsync(dbContext, ct);

            await transaction.CommitAsync(ct);
            logger.LogCompleted(parks.Count, summary.Inserted, summary.Updated, summary.Deactivated);
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }
        finally
        {
            appLifetime.StopApplication();
        }
    }

    private static async Task InsertRowsAsync(HrdDbContext dbContext, IEnumerable<PotaParkImportRow> rows, CancellationToken ct)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
INSERT INTO [dbo].[PotaParksImport]
([reference], [name], [latitude], [longitude], [grid], [locationDesc], [attempts], [activations], [qsos], [Country])
VALUES
(@reference, @name, @latitude, @longitude, @grid, @locationDesc, @attempts, @activations, @qsos, @country);
""";

        var reference = AddParameter(command, "@reference", DbType.AnsiString, 20);
        var name = AddParameter(command, "@name", DbType.String, 500);
        var latitude = AddParameter(command, "@latitude", DbType.Decimal);
        var longitude = AddParameter(command, "@longitude", DbType.Decimal);
        var grid = AddParameter(command, "@grid", DbType.AnsiString, 6);
        var locationDesc = AddParameter(command, "@locationDesc", DbType.AnsiString, 200);
        var attempts = AddParameter(command, "@attempts", DbType.Int32);
        var activations = AddParameter(command, "@activations", DbType.Int32);
        var qsos = AddParameter(command, "@qsos", DbType.Int32);
        var country = AddParameter(command, "@country", DbType.String, 2);

        foreach (var row in rows)
        {
            reference.Value = DbValue(row.Reference);
            name.Value = DbValue(row.Name);
            latitude.Value = DbValue(row.Latitude);
            longitude.Value = DbValue(row.Longitude);
            grid.Value = DbValue(row.Grid);
            locationDesc.Value = DbValue(row.LocationDesc);
            attempts.Value = row.Attempts ?? 0;
            activations.Value = row.Activations ?? 0;
            qsos.Value = row.Qsos ?? 0;
            country.Value = DbValue(GetCountryCode(row.Reference));

            await command.ExecuteNonQueryAsync(ct);
        }
    }

    private async Task<ImportSummary> ExecuteImportProcAsync(HrdDbContext dbContext, CancellationToken ct)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "EXEC [dbo].[ImportPotaParks-US]";

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            logger.LogProcSummaryMissing();
            return new ImportSummary(0, 0, 0);
        }

        return new ImportSummary(
            GetIntOrDefault(reader, "Inserted"),
            GetIntOrDefault(reader, "Updated"),
            GetIntOrDefault(reader, "Deactivated"));
    }

    private static int GetIntOrDefault(DbDataReader reader, string columnName)
    {
        var index = reader.GetOrdinal(columnName);
        return reader.IsDBNull(index) ? 0 : reader.GetInt32(index);
    }

    private static DbParameter AddParameter(DbCommand command, string name, DbType dbType, int? size = null)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;

        if (size.HasValue)
            parameter.Size = size.Value;

        command.Parameters.Add(parameter);
        return parameter;
    }

    private static object DbValue<T>(T value)
    {
        return value is null ? DBNull.Value : value;
    }

    private static string? GetCountryCode(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        var dashIndex = reference.IndexOf('-', StringComparison.Ordinal);
        if (dashIndex <= 0)
            return null;

        var countryCode = reference[..dashIndex].Trim();
        return countryCode.Length > 2 ? countryCode[..2] : countryCode;
    }
}





