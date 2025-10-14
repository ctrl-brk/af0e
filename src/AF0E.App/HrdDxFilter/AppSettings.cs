namespace HrdDxFilter;

public class AppSettings
{
    public Uri DxApiUrl { get; init; } = null!;
    public string HrdFiltersFile { get; init; } = null!;
    public string HrdDxFilterTitle { get; init; } = null!;
    public string CustomDxFile { get; init; } = null!;
    public string SaveToFile { get; init; } = null!;
}
