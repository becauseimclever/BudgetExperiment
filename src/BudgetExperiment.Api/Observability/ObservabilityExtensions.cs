// <copyright file="ObservabilityExtensions.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Reflection;

using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// Extension methods for configuring the application observability stack
/// (Serilog structured logging, optional file/Seq sinks, optional OpenTelemetry OTLP export).
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Configures the full observability pipeline: Serilog logging with console (always),
    /// optional file sink, optional Seq sink, and optional OpenTelemetry OTLP export.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        var serviceName = configuration.GetValue("Observability:ServiceName", "BudgetExperiment")!;
        var serviceVersion = configuration.GetValue<string>("Observability:ServiceVersion")
            ?? Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "unknown";

        ConfigureDebugLogBuffer(builder, configuration);
        builder.Services.AddSingleton<ILogSanitizer, LogSanitizer>();
        ConfigureSerilog(builder, configuration, environment);
        ConfigureOpenTelemetry(builder, configuration, serviceName, serviceVersion, environment);

        LogActiveSinks(configuration, environment);

        return builder;
    }

    private static void LogActiveSinks(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var sinks = new List<string> { environment.IsDevelopment() ? "Console (readable)" : "Console (JSON)" };

        if (!string.IsNullOrWhiteSpace(configuration.GetValue<string>("Observability:File:Path")))
        {
            sinks.Add("File");
        }

        if (!string.IsNullOrWhiteSpace(configuration.GetValue<string>("Observability:Seq:Url")))
        {
            sinks.Add("Seq");
        }

        var otlpEndpoint = configuration.GetValue<string>("Observability:Otlp:Endpoint");
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            sinks.Add($"OTLP ({otlpEndpoint})");
        }

        var debugExportEnabled = configuration.GetValue("Observability:DebugExport:Enabled", true);
        if (debugExportEnabled)
        {
            sinks.Add("DebugBuffer");
        }

        Log.Information("Observability sinks active: {ActiveSinks}", string.Join(", ", sinks));
    }

    private static void ConfigureDebugLogBuffer(
        WebApplicationBuilder builder,
        IConfiguration configuration)
    {
        var enabled = configuration.GetValue("Observability:DebugExport:Enabled", true);
        if (!enabled)
        {
            return;
        }

        var bufferSize = configuration.GetValue("Observability:DebugExport:BufferSize", 1000);
        var retentionSeconds = configuration.GetValue("Observability:DebugExport:RetentionSeconds", 300);

        var buffer = new DebugLogBuffer(bufferSize, TimeSpan.FromSeconds(retentionSeconds));
        builder.Services.AddSingleton<IDebugLogBuffer>(buffer);
    }

    private static void ConfigureSerilog(
        WebApplicationBuilder builder,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            // Read base config from Serilog section (min levels, overrides)
            loggerConfiguration.ReadFrom.Configuration(context.Configuration);

            // Enrichment
            var appVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

            loggerConfiguration
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("ApplicationVersion", appVersion)
                .Enrich.FromLogContext();

            // Console sink — human-readable in Development, compact JSON otherwise
            if (environment.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Console();
            }
            else
            {
                loggerConfiguration.WriteTo.Console(new RenderedCompactJsonFormatter());
            }

            // Optional file sink
            var filePath = configuration.GetValue<string>("Observability:File:Path");
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var fileSizeLimit = configuration.GetValue("Observability:File:FileSizeLimitBytes", 10_485_760L);
                var retainedFileCount = configuration.GetValue("Observability:File:RetainedFileCountLimit", 5);

                loggerConfiguration.WriteTo.File(
                    new RenderedCompactJsonFormatter(),
                    filePath,
                    fileSizeLimitBytes: fileSizeLimit,
                    retainedFileCountLimit: retainedFileCount,
                    rollOnFileSizeLimit: true,
                    shared: false);
            }

            // Optional debug buffer sink
            var debugBuffer = services.GetService<IDebugLogBuffer>();
            if (debugBuffer != null)
            {
                loggerConfiguration.WriteTo.Sink(new DebugBufferSink(debugBuffer));
            }

            // Optional Seq sink
            var seqUrl = configuration.GetValue<string>("Observability:Seq:Url");
            if (!string.IsNullOrWhiteSpace(seqUrl))
            {
                var seqApiKey = configuration.GetValue<string>("Observability:Seq:ApiKey");
                loggerConfiguration.WriteTo.Seq(seqUrl, apiKey: seqApiKey);
            }
        });
    }

    private static void ConfigureOpenTelemetry(
        WebApplicationBuilder builder,
        IConfiguration configuration,
        string serviceName,
        string serviceVersion,
        IWebHostEnvironment environment)
    {
        var otlpEndpoint = configuration.GetValue<string>("Observability:Otlp:Endpoint");
        if (string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            return;
        }

        var protocolStr = configuration.GetValue("Observability:Otlp:Protocol", "grpc")!;
        var otlpProtocol = string.Equals(protocolStr, "http/protobuf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocolStr, "httpprotobuf", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;

        var headers = configuration.GetValue<string>("Observability:Otlp:Headers") ?? string.Empty;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment.name"] = environment.EnvironmentName,
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(otlpEndpoint);
                        o.Protocol = otlpProtocol;
                        if (!string.IsNullOrWhiteSpace(headers))
                        {
                            o.Headers = headers;
                        }
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(otlpEndpoint);
                        o.Protocol = otlpProtocol;
                        if (!string.IsNullOrWhiteSpace(headers))
                        {
                            o.Headers = headers;
                        }
                    });
            })
            .WithLogging(logging =>
            {
                logging.AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otlpEndpoint);
                    o.Protocol = otlpProtocol;
                    if (!string.IsNullOrWhiteSpace(headers))
                    {
                        o.Headers = headers;
                    }
                });
            });
    }
}
