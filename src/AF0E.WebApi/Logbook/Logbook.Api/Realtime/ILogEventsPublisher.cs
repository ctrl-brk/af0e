namespace Logbook.Api.Realtime;

public interface ILogEventsPublisher
{
    Task PublishAsync(LogChangedEvent evt, CancellationToken ct = default);
}

