using Microsoft.Extensions.DependencyInjection;

namespace Shared.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<EventPublisher>();
        return services;
    }
}
