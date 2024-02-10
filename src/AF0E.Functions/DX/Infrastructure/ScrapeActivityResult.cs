namespace AF0E.Functions.DX.Infrastructure;

public class ScrapeActivityResult
{
    public bool IsSuccess { get; init; }
    public List<DxInfo>? Result { get; init; }
}
