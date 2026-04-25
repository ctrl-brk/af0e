// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Logbook.Api.Security;

public sealed class ApiKeyAuthSettings
{
    public bool Enabled { get; init; }
    public string HeaderName { get; init; } = "X-Api-Key";
    public string? Key { get; init; }
}
