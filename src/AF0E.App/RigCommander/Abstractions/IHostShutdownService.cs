namespace RigCommander.Abstractions;

public interface IHostShutdownService
{
    Task StopAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}
