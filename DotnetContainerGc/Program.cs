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

var peopleStore = new List<Person>();

app.MapPost("/people", (Person person) => peopleStore.Add(person));
app.MapGet("/people", () => peopleStore);

app.Run();

public record Person(
    string Name, 
    int Age, 
    DateTime CreatedAt, 
    Address Address, 
    Dictionary<string, string>? Metadata = null // Optional key-value store
);

public record Address(
    string Street,
    string City,
    string State,
    string Country,
    string PostalCode
);
