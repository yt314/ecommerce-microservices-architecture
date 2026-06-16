using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Shared.Messaging;

/// <summary>
/// Publishes events to the shared topic exchange as persistent JSON messages.
/// A fresh channel is opened per publish (channels are not thread-safe); this is
/// perfectly fine at the volumes of a course demo.
/// </summary>
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

        // Carry the ambient correlation id with the message so the saga can be
        // traced across the broker. RabbitMQ's native CorrelationId property is
        // the natural place for it; the consumer reads it back on the other side.
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
