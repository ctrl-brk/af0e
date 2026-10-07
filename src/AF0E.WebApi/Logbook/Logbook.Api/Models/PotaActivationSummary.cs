using System.Diagnostics.CodeAnalysis;
using AF0E.DB.Models;

namespace Logbook.Api.Models;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed class PotaActivationSummary(PotaActivation activation)
{
    public int Id { get; } = activation.ActivationId;
    public DateTime StartDate { get; } = activation.StartDate;
    public DateTime? EndDate { get; } = activation.EndDate;
    public string ParkNum { get; } = activation.Park.ParkNum;
    public string ParkName { get; } = activation.Park.ParkName;
    public string State { get; } = activation.State;
    public string StationCallsign { get; } = activation.StationCallsign;
    public string OperatorCallsign { get; } = activation.OperatorCallsign;
    public int Count { get; } = activation.PotaContacts.Count;
    public int CwCount { get; } = activation.PotaContacts.Count(c => c.Log.ColMode == "CW");
    public int DigiCount { get; } = activation.PotaContacts.Count(c => c.Log.ColMode is "FT8" or "MFSK");
    public int PhoneCount { get; } = activation.PotaContacts.Count(c => c.Log.ColMode is "SSB" or "LSB" or "USB" or "FM" or "AM");
}
