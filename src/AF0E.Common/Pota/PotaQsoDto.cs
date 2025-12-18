using System.Text.Json.Serialization;

namespace AF0E.Common.Pota;

public class PotaLogResponse
{
    public int Count { get; init; }
    public ICollection<PotaLogEntry> Entries { get; init; } = null!;
}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
public class PotaLogEntry
{
    public long QsoId { get; set; }
    public int UserId { get; set; }
    public DateTime QsoDateTime { get; set; }
    [JsonPropertyName("station_callsign")]
    public string StationCallsign { get; set; }
    [JsonPropertyName("operator_callsign")]
    public string OperatorCallsign { get; set; }
    [JsonPropertyName("worked_callsign")]
    public string WorkedCallsign { get; set; }
    public string Band { get; set; }
    public string Mode { get; set; }
    [JsonPropertyName("rst_sent")]
    public object RstSent { get; set; }
    [JsonPropertyName("rst_rcvd")]
    public object RstRcvd { get; set; }
    [JsonPropertyName("my_sig")]
    public string MySig { get; set; }
    [JsonPropertyName("my_sig_info")]
    public string MySigInfo { get; set; }
    public int? P2pMatch { get; set; }
    public int JobId { get; set; }
    public int ParkId { get; set; }
    public string Reference { get; set; }
    public string Name { get; set; }
    public string ParkTypeDesc { get; set; }
    public int LocationId { get; set; }
    public string LocationDesc { get; set; }
    public string LocationName { get; set; }
    public string Sig { get; set; }
    [JsonPropertyName("sig_info")]
    public string SigInfo { get; set; }
    public string LoggedMode { get; set; }
}
