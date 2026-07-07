using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Shared.Messaging;

// A fresh channel is opened per publish because RabbitMQ channels aren't
// thread-safe to share across concurrent publishers.
public class EventPublisher
{
    private readonly RabbitMqConnection _connection;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(RabbitMqConnection connection, ILogger<EventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public void Publish<T>(string routingKey, T message)
    {
        using var channel = _connection.CreateChannel();
        channel.ExchangeDeclare(EventBusConstants.Exchange, ExchangeType.Topic, durable: true);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        // Stamped onto RabbitMQ's native CorrelationId property (not a custom
        // header) so the consumer on the other side can read it back directly.
        var correlationId = CorrelationContext.Current ?? Guid.NewGuid().ToString();

        var props = channel.CreateBasicProperties();
        props.Persistent = true;                 // survive a broker restart (with durable queues)
        props.ContentType = "application/json";
        props.MessageId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;

        channel.BasicPublish(EventBusConstants.Exchange, routingKey, props, body);
        _logger.LogInformation("PUBLISH [{RoutingKey}] CorrelationId={CorrelationId} {Payload}",
            routingKey, correlationId, JsonSerializer.Serialize(message));
    }
}
