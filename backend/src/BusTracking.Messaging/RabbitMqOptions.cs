namespace BusTracking.Messaging;

/// <summary>
/// Bound to the <c>RabbitMq</c> configuration section. The password comes from User Secrets
/// or the <c>RabbitMq__Password</c> environment variable, never from appsettings.json.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "bus";

    public string Password { get; set; } = string.Empty;

    public string VirtualHost { get; set; } = "/";
}
