using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shared.Messaging;

// prefetch=1 processes one message at a time per queue — deliberately slower,
// but it keeps the idempotency checks in each handler race-free without extra
// locking. A failed handler nacks without requeue (see below), so it's dropped
// rather than retried forever.
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

    protected abstract string QueueName { get; }
    protected abstract string[] RoutingKeys { get; }

    // Throwing here nacks the message without requeue — it's dropped and
    // logged, not retried. Don't throw for cases you want redelivered.
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

            // Recover the correlation id the publisher attached, so this hop's
            // logs — and any event this handler publishes — keep the same id.
            var correlationId = string.IsNullOrWhiteSpace(ea.BasicProperties?.CorrelationId)
                ? Guid.NewGuid().ToString()
                : ea.BasicProperties!.CorrelationId;
            CorrelationContext.Current = correlationId;

            // A BeginScope key/value pair surfaces as a structured Serilog property
            // (CorrelationId / MessageType) on every log written while handling.
            using var logScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["MessageType"] = routingKey
            });

            try
            {
                _logger.LogInformation("CONSUME [{RoutingKey}] on {Queue} CorrelationId={CorrelationId}.",
                    routingKey, QueueName, correlationId);
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
