using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shared.Messaging;

/// <summary>
/// Base class for a background consumer bound to one durable queue. Subclasses
/// declare the queue name + routing keys and implement the message handler.
///
/// Key behaviours:
///   - prefetch = 1  → one message processed at a time (sequential, simple idempotency).
///   - manual ack    → ack only after the handler succeeds; nack (drop) on error.
///   - a DI scope is created per message so handlers can use scoped services (DbContext).
/// </summary>
public abstract class RabbitMqConsumerBase : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private IModel? _channel;

    protected RabbitMqConsumerBase(RabbitMqConnection connection, IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>The durable queue this consumer reads from.</summary>
    protected abstract string QueueName { get; }

    /// <summary>Routing keys to bind the queue to on the shared exchange.</summary>
    protected abstract string[] RoutingKeys { get; }

    /// <summary>Handle one message. Throw to nack (the message is dropped and logged).</summary>
    protected abstract Task HandleAsync(string routingKey, string body, IServiceProvider services, CancellationToken ct);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateChannel();
        _channel.ExchangeDeclare(EventBusConstants.Exchange, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        foreach (var key in RoutingKeys)
            _channel.QueueBind(QueueName, EventBusConstants.Exchange, key);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var routingKey = ea.RoutingKey;
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await HandleAsync(routingKey, body, scope.ServiceProvider, stoppingToken);
                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling [{RoutingKey}] on {Queue}; dropping message.", routingKey, QueueName);
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(QueueName, autoAck: false, consumer);
        _logger.LogInformation("Consuming queue {Queue} bound to [{Keys}].", QueueName, string.Join(", ", RoutingKeys));
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
