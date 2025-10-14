namespace HrdDxFilter;

public class CustomDxInfo
{
    public DateTime BeginDate { get; set; }
    public DateTime EndDate { get; set; }
    public string CallSigns { get; set; } = null!;
    public string? Comment { get; set; }
}
