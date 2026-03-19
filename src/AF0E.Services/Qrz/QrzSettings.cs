namespace AF0E.Services.Qrz;

public sealed record QrzSettings
{
    public required Uri ApiUrl { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}
