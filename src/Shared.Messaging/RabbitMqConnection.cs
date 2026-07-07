using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Shared.Messaging;

// Retries on startup because RabbitMQ is often still booting when this
// service's container starts.
public class RabbitMqConnection : IDisposable
{
    private readonly IConnection _connection;

    public RabbitMqConnection(IConfiguration config, ILogger<RabbitMqConnection> logger)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMq:Host"] ?? "localhost",
            UserName = config["RabbitMq:Username"] ?? "guest",
            Password = config["RabbitMq:Password"] ?? "guest",
            Port = int.Parse(config["RabbitMq:Port"] ?? "5672"),
            // Required so we can use AsyncEventingBasicConsumer (async handlers).
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };

        const int maxAttempts = 12;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                _connection = factory.CreateConnection();
                logger.LogInformation("Connected to RabbitMQ at {Host}.", factory.HostName);
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "RabbitMQ not ready (attempt {Attempt}/{Max}). Retrying in 5s...",
                    attempt, maxAttempts);
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
    }

    public IModel CreateChannel() => _connection.CreateModel();

    public void Dispose() => _connection?.Dispose();
}
