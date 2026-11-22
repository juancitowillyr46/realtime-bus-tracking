namespace BusTracking.Messaging;

/// <summary>
/// Wraps the different RabbitMQ client exceptions so callers handle a single type.
/// </summary>
public sealed class MessagePublishException(string message, Exception innerException)
    : Exception(message, innerException);
