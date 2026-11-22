using System.Collections.Concurrent;
using System.Text;
using BusTracking.Contracts;
using BusTracking.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace BusTracking.Api.IntegrationTests;

/// <summary>
/// Exercises <see cref="RabbitMqConsumer{T}"/> against a real broker: retries, the dead letter
/// queue and idempotency. Each test uses its own route id, so its queue only sees its messages.
/// </summary>
public class RabbitMqConsumerTests : IClassFixture<RabbitMqFixture>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly RabbitMqFixture _rabbitMq;

    public RabbitMqConsumerTests(RabbitMqFixture rabbitMq)
    {
        _rabbitMq = rabbitMq;
    }

    [Fact]
    public async Task HandledMessage_IsAcknowledged()
    {
        await using var scenario = await Scenario.StartAsync(_rabbitMq, fail: false);

        var eventId = await scenario.PublishAsync();

        await WaitUntilAsync(() => scenario.Consumer.Attempts(eventId) == 1);
        await WaitUntilAsync(async () => await scenario.QueueMessageCountAsync() == 0);
    }

    [Fact]
    public async Task MessageThatAlwaysFails_MovesToDeadLetterQueueAfterMaxAttempts()
    {
        await using var scenario = await Scenario.StartAsync(_rabbitMq, fail: true);

        var eventId = await scenario.PublishAsync();

        Assert.True(await scenario.WaitForDeadLetterAsync(eventId), "The message never reached tracking.dead-letter.");
        Assert.Equal(TopologyInitializer.MaxDeliveryAttempts, scenario.Consumer.Attempts(eventId));
    }

    [Fact]
    public async Task UnreadableMessage_GoesStraightToDeadLetterQueue()
    {
        await using var scenario = await Scenario.StartAsync(_rabbitMq, fail: false);

        var eventId = Guid.NewGuid();
        await scenario.PublishRawAsync(eventId, "{ not json");

        Assert.True(await scenario.WaitForDeadLetterAsync(eventId), "The message never reached tracking.dead-letter.");
        Assert.Equal(0, scenario.Consumer.Attempts(eventId));
    }

    [Fact]
    public async Task SameEventIdTwice_IsHandledOnce()
    {
        await using var scenario = await Scenario.StartAsync(_rabbitMq, fail: false);

        var eventId = Guid.NewGuid();
        await scenario.PublishAsync(eventId);
        await scenario.PublishAsync(eventId);

        // Messages are handled one at a time and in order, so once a later message is
        // handled, both copies of the duplicate have been through the consumer.
        var sentinel = await scenario.PublishAsync();
        await WaitUntilAsync(() => scenario.Consumer.Attempts(sentinel) == 1);

        Assert.Equal(1, scenario.Consumer.Attempts(eventId));
    }

    [Fact]
    public async Task ConsumerQueue_IsDeclaredAsDurableQuorumQueue()
    {
        await using var scenario = await Scenario.StartAsync(_rabbitMq, fail: false);

        // Redeclaring with different arguments would fail, so this proves the queue's settings.
        await TopologyInitializer.DeclareConsumerQueueAsync(scenario.Channel, scenario.QueueName, scenario.Pattern);
        var declared = await scenario.Channel.QueueDeclarePassiveAsync(scenario.QueueName);

        Assert.Equal(scenario.QueueName, declared.QueueName);
    }

    private static async Task WaitUntilAsync(Func<bool> condition) =>
        await WaitUntilAsync(() => Task.FromResult(condition()));

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTimeOffset.UtcNow + Timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Fail("The condition was not met in time.");
    }

    private sealed class TestConsumer : RabbitMqConsumer<LocationUpdated>
    {
        private readonly ConcurrentDictionary<Guid, int> _attempts = new();
        private readonly bool _fail;

        public TestConsumer(RabbitMqConnectionProvider provider, string queueName, string pattern, bool fail)
            : base(provider, NullLogger.Instance)
        {
            QueueName = queueName;
            BindingPattern = pattern;
            _fail = fail;
        }

        protected override string QueueName { get; }

        protected override string BindingPattern { get; }

        public int Attempts(Guid eventId) => _attempts.GetValueOrDefault(eventId);

        protected override Task HandleAsync(LocationUpdated message, CancellationToken cancellationToken)
        {
            _attempts.AddOrUpdate(message.EventId, 1, (_, count) => count + 1);
            return _fail ? throw new InvalidOperationException("Simulated failure") : Task.CompletedTask;
        }
    }

    private sealed class Scenario : IAsyncDisposable
    {
        private readonly RabbitMqConnectionProvider _consumerProvider;
        private readonly RabbitMqConnectionProvider _publisherProvider;
        private readonly RabbitMqEventPublisher _publisher;
        private readonly IConnection _connection;
        private readonly string _routeId;

        private Scenario(
            RabbitMqFixture rabbitMq,
            IConnection connection,
            IChannel channel,
            bool fail)
        {
            _routeId = $"route-{Guid.NewGuid():N}";
            QueueName = $"test.{_routeId}";
            Pattern = TrackingTopology.LocationUpdatedRoutingKey(_routeId);
            _connection = connection;
            Channel = channel;
            _consumerProvider = rabbitMq.CreateConnectionProvider();
            _publisherProvider = rabbitMq.CreateConnectionProvider();
            _publisher = new RabbitMqEventPublisher(_publisherProvider);
            Consumer = new TestConsumer(_consumerProvider, QueueName, Pattern, fail);
        }

        public string QueueName { get; }

        public string Pattern { get; }

        public IChannel Channel { get; }

        public TestConsumer Consumer { get; }

        public static async Task<Scenario> StartAsync(RabbitMqFixture rabbitMq, bool fail)
        {
            var connection = await rabbitMq.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            var scenario = new Scenario(rabbitMq, connection, channel, fail);

            // Declare first, so nothing published before the consumer is ready gets lost.
            await TopologyInitializer.DeclareConsumerQueueAsync(channel, scenario.QueueName, scenario.Pattern);
            await scenario.Consumer.StartAsync(CancellationToken.None);
            return scenario;
        }

        public async Task<Guid> PublishAsync(Guid? eventId = null)
        {
            var id = eventId ?? Guid.NewGuid();
            var message = new LocationUpdated(id, "bus-test", _routeId, 4.8133, -75.6961, 25, 270, DateTimeOffset.UtcNow);
            await _publisher.PublishAsync(message, Pattern, id);
            return id;
        }

        public async Task PublishRawAsync(Guid eventId, string body)
        {
            var properties = new BasicProperties
            {
                MessageId = eventId.ToString(),
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
            };
            await Channel.BasicPublishAsync(TrackingTopology.ExchangeName, Pattern, false, properties, Encoding.UTF8.GetBytes(body));
        }

        public async Task<uint> QueueMessageCountAsync() =>
            (await Channel.QueueDeclarePassiveAsync(QueueName)).MessageCount;

        /// <summary>
        /// Looks for <paramref name="eventId"/> in the shared dead letter queue, putting back
        /// any other test's messages it reads on the way.
        /// </summary>
        public async Task<bool> WaitForDeadLetterAsync(Guid eventId)
        {
            var deadline = DateTimeOffset.UtcNow + Timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                var message = await Channel.BasicGetAsync(TrackingTopology.DeadLetterQueueName, autoAck: false);
                if (message is null)
                {
                    await Task.Delay(100);
                    continue;
                }

                if (message.BasicProperties.MessageId == eventId.ToString())
                {
                    await Channel.BasicAckAsync(message.DeliveryTag, multiple: false);
                    return true;
                }

                await Channel.BasicNackAsync(message.DeliveryTag, multiple: false, requeue: true);
            }

            return false;
        }

        public async ValueTask DisposeAsync()
        {
            await Consumer.StopAsync(CancellationToken.None);
            Consumer.Dispose();
            await _publisher.DisposeAsync();
            await _publisherProvider.DisposeAsync();
            await _consumerProvider.DisposeAsync();
            await Channel.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
