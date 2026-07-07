using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Shared.Observability;

public static class ObservabilityExtensions
{
    // "Seq:ServerUrl" maps to env var Seq__ServerUrl in docker-compose (the
    // double underscore is .NET config's convention for nested keys). Falls
    // back to localhost so a service still logs somewhere when run outside Docker.
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

    // Lets docker-compose's healthcheck run `dotnet <Service>.dll --healthcheck`
    // as a short-lived self-probe instead of starting the full web host — no
    // curl or extra tooling needed in the runtime image.
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
