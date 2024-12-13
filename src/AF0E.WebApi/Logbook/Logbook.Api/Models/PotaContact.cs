namespace Log.Api.Models;

internal sealed class PotaContact
{
    public int ContactId { get; init; }
    public int LogId { get; init; }
    public int ActivationId { get; init; }
    public string? P2P { get; init; }

    public PotaActivation Activation { get; init; } = null!;
    public HrdLog Log { get; init; } = null!;
}
