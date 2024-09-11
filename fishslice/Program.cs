using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;
using System.Text.Json.Serialization;
using fishslice;
using fishslice.Converters;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Swashbuckle.AspNetCore.Filters;
using ILogger = Microsoft.Extensions.Logging.ILogger;

var builder = WebApplication.CreateBuilder(args);

var configurationManager = builder.Configuration;

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .WriteTo.Console(new JsonFormatter())
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddHealthChecks();

var mvcBuilder = builder.Services.AddControllers();
mvcBuilder.Services.AddOptionsWithValidateOnStart<Configuration>()
    .BindConfiguration("fishsliceConfig");
mvcBuilder.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new PreScrapeActionConverter());
});
mvcBuilder.Services.AddSingleton<Instrumentation>();

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metricsProviderBuilder =>
    {
        metricsProviderBuilder
            .ConfigureResource(resourceBuilder =>
            {
                var config = configurationManager.GetSection("Otlp").Get<OtelServiceConfiguration>();
                resourceBuilder.AddService(config.ServiceName)
                    .AddTelemetrySdk();
            })
            .AddRuntimeInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddProcessInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"] ?? string.Empty);
            });
        var meterName = configurationManager["fishsliceConfig:MeterName"] ??
                        throw new NullReferenceException("Meter missing a name");
        metricsProviderBuilder.AddMeter(meterName);
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.ExampleFilters();
    c.CustomSchemaIds(x => x.FullName?.Replace("+", "_"));
});

builder.Services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());

builder.WebHost.UseUrls("http://*:80");

var app = builder.Build();

app.UseSerilogRequestLogging(opts =>
{
    //opts.EnrichDiagnosticContext = LogHelper.EnrichFromRequest;
    //opts.GetLevel = LogHelper.ExcludeHealthChecks;
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = false
});

app.UseMiddleware<ServiceVersionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    c.RoutePrefix = string.Empty;
});
app.UseDeveloperExceptionPage();
app.MapControllers();
   
var logger = app.Services.GetRequiredService<ILogger<Program>>();
LogStartupMessage(logger);
app.Run();


internal partial class Program
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting service")]
    static partial void LogStartupMessage(ILogger logger);
}