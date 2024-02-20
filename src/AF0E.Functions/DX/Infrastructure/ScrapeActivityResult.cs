namespace AF0E.Functions.DX.Infrastructure;

public sealed class ScrapeActivityResult
{
    public bool IsSuccess { get; init; }
#pragma warning disable CA1002
    public List<DxInfo>? Result { get; init; }
#pragma warning restore CA1002
}
