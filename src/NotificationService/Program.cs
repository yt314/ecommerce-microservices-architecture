using NotificationService.Data;
using Serilog;
using Shared.Messaging;
using Shared.Observability;
using StackExchange.Redis;

// docker-compose healthcheck entrypoint: probe /health and exit (no web host).
ObservabilityExtensions.RunHealthProbeIfRequested(args);

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("NotificationService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "NotificationService", Version = "v1" }));

builder.Services.AddRabbitMqMessaging();
builder.Services.AddHostedService<NotificationSagaConsumer>();

// AbortOnConnectFail=false so the service still starts if Redis is mid-boot;
// StackExchange.Redis reconnects automatically once it's up.
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
var options = ConfigurationOptions.Parse(redisConnection);
options.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options));
builder.Services.AddScoped<NotificationStore>();

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "NotificationService v1");
    o.RoutePrefix = string.Empty;
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "NotificationService" }));

app.Run();
