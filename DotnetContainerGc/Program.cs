using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry().ConfigureResource(resource => resource
        .AddService(serviceName: builder.Environment.ApplicationName))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddProcessInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();
app.MapPrometheusScrapingEndpoint();

var peopleStore = new ConcurrentDictionary<Guid, Person>();

app.MapGet("/people/{id:guid}",
    async (Guid id) =>
    {
        return await Task.Run(() =>
            peopleStore.TryGetValue(id, out var person) 
                ? Results.Ok((object?)person) 
                : Results.NotFound());
    });
app.MapPost("/people", async (Person person) =>
{
    return await Task.Run(() =>
    {
        var id = Guid.NewGuid();
        person = person with { Id = id };
        var metadata = new Dictionary<string, string>
        {
            { "test", Helpers.CreateRandomString() },
        };
        person = person with { Metadata = metadata };
        peopleStore[id] = person;

        return Results.Created($"/people/{id}", person);
    });
});
app.MapDelete("/people/{id:guid}", async (Guid id) =>
{
    return await Task.Run(() => peopleStore.TryRemove(id, out var person) 
        ? Results.Ok((object?)person) 
        : Results.NotFound());
});

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