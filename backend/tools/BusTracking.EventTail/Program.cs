using BusTracking.EventTail;
using BusTracking.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Usage: dotnet run -- [--pattern <binding pattern>]   e.g. --pattern location.updated.route-01
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(new EventTailSettings(builder.Configuration["pattern"] ?? "#"));
builder.Services.AddHostedService<EventTailWorker>();

await builder.Build().RunAsync();
