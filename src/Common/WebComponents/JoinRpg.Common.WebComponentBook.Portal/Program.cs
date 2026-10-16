using JoinRpg.Common.WebInfrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJoinHealthChecks();
builder.Services.ConfigureForwardedHeaders();
builder.Host.UseJoinSerilog("ComponentBook");

var app = builder.Build();

app.UseForwardedHeaders();

// Маршрутизация — до логирования запросов: эндпойнты с ShortCircuit() замыкаются
// прямо в EndpointRoutingMiddleware и не попадают в лог (как в Portal и IdPortal).
app.UseRouting();

app.UseJoinRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapJoinHealthChecks();

app.MapFallbackToFile("index.html");

app.Run();
