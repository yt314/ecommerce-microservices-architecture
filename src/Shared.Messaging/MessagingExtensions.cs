using Microsoft.Extensions.DependencyInjection;

namespace Shared.Messaging;

/// <summary>DI helper so each service can wire RabbitMQ in one line.</summary>
public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<EventPublisher>();
        return services;
    }
}
