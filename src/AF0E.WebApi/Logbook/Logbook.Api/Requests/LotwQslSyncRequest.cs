namespace Logbook.Api.Requests;

public sealed record LotwQslSyncRequest
{
    public DateOnly? Date { get; init; }
}
