using AF0E.Common.Qrz;
using AF0E.Services.Qrz;

namespace Logbook.Api.Handlers;

public static class QrzHandlers
{
    public static async Task<(QrzDatabase?, bool)> Lookup(string callSign, IQrzService qrzService, CancellationToken ct) =>
        await qrzService.QueryCallsignAsync(callSign, ct);
}
