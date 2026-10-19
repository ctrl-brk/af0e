using System.Diagnostics.CodeAnalysis;
using AF0E.DB.Models;

namespace Logbook.Api.Models;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed class PotaActivationDetails(PotaActivation activation)
{
    public int Id { get; } = activation.ActivationId;
    public DateTime StartDate { get; } = activation.StartDate;
    public DateTime? EndDate { get; } = activation.EndDate;
    public DateTime? LogSubmittedDate { get; } = activation.LogSubmittedDate;
    public string ParkNum { get; } = activation.Park.ParkNum;
    public string ParkName { get; } = activation.Park.ParkName;
    public string? SiteComments { get; } = activation.SiteComments;
    public string? City { get; } = activation.City;
    public string County { get; } = activation.County;
    public string State { get; } = activation.State;
    public string Grid { get; } = activation.Grid;
    public decimal Lat { get; } = activation.Lat;
#pragma warning disable CA1720 // Preserve the existing API property name.
    public decimal Long { get; } = activation.Long;
#pragma warning restore CA1720
    public string StationCallsign { get; } = activation.StationCallsign;
    public string OperatorCallsign { get; } = activation.OperatorCallsign;
    public string Status { get; } = activation.Status.ToString();
    public int Count { get; } = activation.PotaContacts.Count;
    public int CwCount { get; } = activation.PotaContacts.Count(c => c.Log.ColMode == "CW");
    public int DigiCount { get; } = activation.PotaContacts.Count(c => c.Log.ColMode is "FT8" or "MFSK");
    public int PhoneCount { get; } = activation.PotaContacts.Count(c => c.Log.ColMode is "SSB" or "LSB" or "USB" or "FM" or "AM");
    // ReSharper disable once InconsistentNaming
    public int P2pCount { get; } = activation.PotaContacts.Count(c => c.Log.PotaHunting.Count > 0);
}
