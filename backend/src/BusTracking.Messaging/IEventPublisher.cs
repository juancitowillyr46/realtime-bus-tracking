namespace BusTracking.Messaging;

public interface IEventPublisher
{
    /// <summary>
    /// Publishes <paramref name="message"/> to the tracking exchange and waits for the broker to confirm it.
    /// </summary>
    /// <exception cref="MessagePublishException">The broker is unreachable or did not confirm the message.</exception>
    Task PublishAsync<T>(T message, string routingKey, Guid messageId, CancellationToken cancellationToken = default);
}
