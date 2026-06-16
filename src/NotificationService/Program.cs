using NotificationService.Data;
using Shared.Messaging;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "NotificationService", Version = "v1" }));

// RabbitMQ connection + the consumer that records final order notifications.
builder.Services.AddRabbitMqMessaging();
builder.Services.AddHostedService<NotificationSagaConsumer>();

// Redis connection (singleton multiplexer). AbortOnConnectFail=false lets the
// service start even if Redis is still booting; it reconnects automatically.
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
var options = ConfigurationOptions.Parse(redisConnection);
options.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options));
builder.Services.AddScoped<NotificationStore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "NotificationService v1");
    o.RoutePrefix = string.Empty;
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "NotificationService" }));

app.Run();
