using AF0E.Common.Qrz;

namespace Logbook.Api.Responses;

public sealed record QrzResponse(QrzDatabase? qrzResult, bool notFound);
