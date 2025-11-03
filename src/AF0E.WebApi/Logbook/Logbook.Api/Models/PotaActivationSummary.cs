using AF0E.DB.Models;

namespace Logbook.Api.Models;

public sealed class PotaActivationSummary(PotaActivation activation)
{
    public int Id { get; set; } = activation.ActivationId;
    public DateTime StartDate { get; set; } = activation.StartDate;
    public DateTime? EndDate { get; set; } = activation.EndDate;
    public string ParkNum { get; set; } = activation.Park.ParkNum;
    public string ParkName { get; set; } = activation.Park.ParkName;
    public int Count { get; set; } = activation.PotaContacts.Count;
    public int CwCount { get; set; } = activation.PotaContacts.Count(c => c.Log.ColMode == "CW");
    public int DigiCount { get; set; } = activation.PotaContacts.Count(c => c.Log.ColMode is "FT8" or "MFSK");
    public int PhoneCount { get; set; } = activation.PotaContacts.Count(c => c.Log.ColMode is "SSB" or "LSB" or "USB" or "FM" or "AM");
}
