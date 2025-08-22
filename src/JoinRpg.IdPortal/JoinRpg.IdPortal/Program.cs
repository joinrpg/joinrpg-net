using JoinRpg.BlobStorage;
using JoinRpg.Common.EmailSending.Impl;
using JoinRpg.Common.WebInfrastructure.Logging;
using JoinRpg.Dal.Impl;
using JoinRpg.IdPortal;
using JoinRpg.IdPortal.Components;
using JoinRpg.IdPortal.Components.Account;
using JoinRpg.Interfaces;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
{
    var loggerOptions = context.Configuration.GetSection("Logging").Get<SerilogOptions>();
    configuration.ConfigureLogger(loggerOptions!, "JoinRpg.IdPortal");
});


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddJoinDal();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddSingleton<IJoinDbContextConfiguration, ConfigurationAdapter>();

builder.Services
    .Configure<JoinRpgHostNamesOptions>(builder.Configuration.GetSection("JoinRpgHostNames"))
    .Configure<S3StorageOptions>(builder.Configuration.GetSection("S3BlobStorage"))
    .Configure<NotificationsOptions>(builder.Configuration.GetSection("Notifications"));

builder.Services.AddJoinExternalLogins(builder.Configuration.GetSection("Authentication"));

builder.Services.ConfigureForwardedHeaders();

builder.Services.AddUserServicesOnly();

builder.Services
    .AddJoinIdentity()
    .AddJoinBlobStorage()
    .AddJoinEmailSendingService();

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

//app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    logger.LogInformation("Scheme: {scheme}", context.Request.Scheme);
    logger.LogInformation("Host: " + context.Request.Host);
    foreach (var header in context.Request.Headers)
    {
        logger.LogInformation($"{header.Key}: {header.Value}");
    }
    await next();
});

app.UseJoinRequestLogging();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(JoinRpg.IdPortal.Client._Imports).Assembly);

app.MapJoinHealthChecks();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
