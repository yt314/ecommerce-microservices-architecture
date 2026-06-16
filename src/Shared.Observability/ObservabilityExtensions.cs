using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Shared.Observability;

/// <summary>
/// One-call setup for Phase 5 observability so the services stay almost unchanged:
///   - structured logging via Serilog to console + Seq, enriched with the service
///     name and (from LogContext / log scopes) the CorrelationId;
///   - a self-contained health probe used by the docker-compose healthchecks.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Configures Serilog as the logging provider. Reads the Seq endpoint from
    /// "Seq:ServerUrl" (env var Seq__ServerUrl in docker-compose), defaulting to
    /// localhost for running a service outside Docker.
    /// </summary>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder, string serviceName)
    {
        var seqUrl = builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";

        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] (Corr:{CorrelationId}) {Message:lj}{NewLine}{Exception}")
            .WriteTo.Seq(seqUrl));

        return builder;
    }

    /// <summary>
    /// If the process was started with "--healthcheck" (the docker-compose
    /// healthcheck command), probe this container's own /health endpoint and exit
    /// 0/1 instead of starting the web host. Needs no extra tools in the image —
    /// it reuses the .NET runtime that is already present.
    /// </summary>
    public static void RunHealthProbeIfRequested(string[] args)
    {
        if (!args.Contains("--healthcheck"))
            return;

        var port = Environment.GetEnvironmentVariable("HEALTHCHECK_PORT") ?? "8080";
        var code = 1;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = client.GetAsync($"http://localhost:{port}/health").GetAwaiter().GetResult();
            code = response.IsSuccessStatusCode ? 0 : 1;
        }
        catch
        {
            code = 1;
        }

        Environment.Exit(code);
    }
}
