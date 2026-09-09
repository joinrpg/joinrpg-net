using JoinRpg.Common.WebInfrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.ConfigureForwardedHeaders();
builder.Host.UseJoinSerilog("ComponentBook");

var app = builder.Build();

app.UseForwardedHeaders();

app.UseJoinRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapJoinHealthChecks();

app.MapFallbackToFile("index.html");

app.Run();
