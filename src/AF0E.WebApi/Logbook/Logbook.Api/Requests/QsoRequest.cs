using Logbook.Api.Models;

namespace Logbook.Api.Requests;

public class QsoRequest
{
    public int? PotaActivationId { get; set; }
    public QsoDetails Qso { get; set; } = new();
}
