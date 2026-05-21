using Logbook.Api.Models;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Logbook.Api.Responses;

#pragma warning disable CA2227
#pragma warning disable CA1002
public sealed class LogSearchResponse
{
    public int TotalCount { get; set; }
    public List<QsoSummary> Contacts { get; set; } = [];
}
