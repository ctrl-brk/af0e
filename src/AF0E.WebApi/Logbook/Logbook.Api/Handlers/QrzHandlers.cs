using AF0E.Services.Qrz;
using Logbook.Api.Responses;

namespace Logbook.Api.Handlers;

public static class QrzHandlers
{
    public static async Task<QrzResponse> Lookup(string callSign, IQrzService qrzService, CancellationToken ct)
    {
        var (response, notFound) = await qrzService.QueryCallsignAsync(callSign, ct);
        return new QrzResponse(response, notFound);
    }
}
