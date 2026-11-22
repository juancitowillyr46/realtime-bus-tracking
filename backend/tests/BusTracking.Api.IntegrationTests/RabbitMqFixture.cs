using BusTracking.Messaging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace BusTracking.Api.IntegrationTests;

/// <summary>
/// Starts a throwaway RabbitMQ in Docker once per test class and points the API at it.
/// </summary>
public sealed class RabbitMqFixture : IAsyncLifetime
{
    private const string UserName = "test";
    private const string Password = "test";
    private const int AmqpPort = 5672;

    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4-management")
        .WithUsername(UserName)
        .WithPassword(Password)
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <param name="routesDirectory">Optional <c>Routes:Directory</c>; by default the API finds data/routes.</param>
    public WebApplicationFactory<Program> CreateApiFactory(string? routesDirectory = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RabbitMq:HostName"] = _container.Hostname,
                    ["RabbitMq:Port"] = _container.GetMappedPublicPort(AmqpPort).ToString(),
                    ["RabbitMq:UserName"] = UserName,
                    ["RabbitMq:Password"] = Password,
                    ["RabbitMq:VirtualHost"] = "/",
                    ["Routes:Directory"] = routesDirectory,
                })));

    /// <summary>A connection provider for running a consumer or publisher outside the API.</summary>
    public RabbitMqConnectionProvider CreateConnectionProvider() =>
        new(
            Options.Create(new RabbitMqOptions
            {
                HostName = _container.Hostname,
                Port = _container.GetMappedPublicPort(AmqpPort),
                UserName = UserName,
                Password = Password,
                VirtualHost = "/",
            }),
            NullLogger<RabbitMqConnectionProvider>.Instance);

    public Task<IConnection> CreateConnectionAsync() =>
        new ConnectionFactory
        {
            HostName = _container.Hostname,
            Port = _container.GetMappedPublicPort(AmqpPort),
            UserName = UserName,
            Password = Password,
        }.CreateConnectionAsync();
}
