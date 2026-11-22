using BusTracking.Api;
using BusTracking.Api.Realtime;
using BusTracking.Contracts;
using BusTracking.Core;
using BusTracking.Core.Routes;
using BusTracking.Messaging;

const string ExpoWebCorsPolicy = "ExpoWeb";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddRouteCatalog();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();

builder.Services.AddSignalR();
builder.Services.AddSingleton<BusPositionTracker>();
builder.Services.AddHostedService<RealtimeLocationConsumer>();

// The Expo app in a browser (npx expo start --web) runs on another origin. The iPhone app
// is not a browser, so CORS does not apply to it. Development only: there is no deployment.
// Cors:AllowedOrigins lists the origins (Expo uses 8081, or another port when it is taken).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(ExpoWebCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

// Fail at startup, not on the first request, when the routes are missing or invalid.
app.Services.GetRequiredService<RouteCatalog>();

if (app.Environment.IsDevelopment())
{
    app.UseCors(ExpoWebCorsPolicy);
}

app.MapRouteEndpoints();
app.MapFleetEndpoints();
app.MapHub<TrackingHub>(TrackingHubContract.Path);

app.MapPost("/api/locations", async (
    LocationReport report,
    IEventPublisher publisher,
    TimeProvider clock,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var errors = LocationReportValidator.Validate(report, clock.GetUtcNow());
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors, title: "Los datos de la ubicación no son válidos.");
    }

    var locationUpdated = new LocationUpdated(
        Guid.NewGuid(),
        report.BusId,
        report.RouteId,
        report.Latitude,
        report.Longitude,
        report.SpeedKmh,
        report.Heading,
        report.RecordedAt);

    try
    {
        await publisher.PublishAsync(
            locationUpdated,
            TrackingTopology.LocationUpdatedRoutingKey(locationUpdated.RouteId),
            locationUpdated.EventId,
            cancellationToken);
    }
    catch (MessagePublishException exception)
    {
        logger.LogError(
            exception,
            "Could not publish location for bus {BusId} on route {RouteId}",
            report.BusId,
            report.RouteId);

        return Results.Problem(
            title: "El servicio de mensajería no está disponible. Intenta de nuevo en unos segundos.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    logger.LogInformation(
        "Published location {EventId} for bus {BusId} on route {RouteId}",
        locationUpdated.EventId,
        locationUpdated.BusId,
        locationUpdated.RouteId);

    return Results.Accepted(value: new LocationAccepted(locationUpdated.EventId));
});

app.MapGet("/health", async (
    RabbitMqConnectionProvider connectionProvider,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
        if (connection.IsOpen)
        {
            return Results.Ok(new { status = "healthy" });
        }
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
        logger.LogWarning(exception, "RabbitMQ is not reachable");
    }

    return Results.Json(new { status = "unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.Run();

// Lets WebApplicationFactory<Program> in the integration tests see the entry point.
public partial class Program;
