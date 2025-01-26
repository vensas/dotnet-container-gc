using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// Create meters for tracking GC and memory metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: builder.Environment.ApplicationName))
        .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddProcessInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("DotnetContainerGc.Metrics")
        .AddPrometheusExporter());

var app = builder.Build();
app.MapPrometheusScrapingEndpoint();

var peopleStore = new ConcurrentDictionary<Guid, Person>();
var meter = new Meter("DotnetContainerGc.Metrics");
var itemsMetric = meter.CreateUpDownCounter<long>("people_store_count");
var memoryUsageMetric = meter.CreateObservableGauge("memory_usage_bytes", () => 
    GC.GetTotalMemory(false));

// Simulate a cache to prevent immediate collection
var recentAccesses = new ConcurrentDictionary<Guid, DateTime>();

app.MapGet("/people/{id:guid}", async (Guid id) =>
{
    return await Task.Run(() =>
    {
        if (peopleStore.TryGetValue(id, out var person))
        {
            recentAccesses[id] = DateTime.UtcNow;
            return Results.Ok(person);
        }
        return Results.NotFound();
    });
});

app.MapPost("/people", async (Person person) =>
{
    return await Task.Run(() =>
    {
        var id = Guid.NewGuid();
        person = person with { Id = id };
        var metadata = new Dictionary<string, string>();
        
        // Create larger strings and more of them
        for(int i = 0; i < 10; i++) {
            metadata[$"test_{i}"] = Helpers.CreateRandomString(50000);
        }
        
        person = person with { Metadata = metadata };
        peopleStore[id] = person;
        recentAccesses[id] = DateTime.UtcNow;
        itemsMetric.Add(1);

        // Simulate some work to create memory pressure
        var workBuffer = new byte[1024 * 1024]; // 1MB
        Random.Shared.NextBytes(workBuffer);

        return Results.Created($"/people/{id}", person);
    });
});

// Add a cleanup background task
var cleanupTimer = new Timer(_ =>
{
    var cutoff = DateTime.UtcNow.AddMinutes(-5);
    foreach (var access in recentAccesses)
    {
        if (access.Value < cutoff)
        {
            if (recentAccesses.TryRemove(access.Key, out var result))
            {
                peopleStore.TryRemove(access.Key, out var result2);
                itemsMetric.Add(-1);
            }
        }
    }
}, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

app.Run();

public record Person(
    Guid Id,
    string Name,
    int Age,
    DateTime CreatedAt,
    Dictionary<string, string>? Metadata = null
);

static class Helpers
{
    public static string CreateRandomString(int length = 10000)
    {
        return RandomNumberGenerator.GetString(
            choices: "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",
            length: length);
    }
}