using Logbook.Api.Models;

namespace Logbook.Api.Responses;

public sealed class LogSearchResponse
{
    public int TotalCount { get; set; }
    public List<QsoSummary> Contacts { get; set; } = [];
}
