using JoinRpg.BlobStorage;
using JoinRpg.Common.EmailSending.Impl;
using JoinRpg.Dal.Impl;
using JoinRpg.IdPortal;
using JoinRpg.IdPortal.Components;
using JoinRpg.IdPortal.Components.Account;
using JoinRpg.Interfaces;
using JoinRpg.Services.Impl;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

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
    .Configure<NotificationsOptions>(builder.Configuration.GetSection("Notifications"));

builder.Services.AddJoinExternalLogins(builder.Configuration.GetSection("Authentication"));

builder.Services.AddUserServicesOnly();

builder.Services
    .AddJoinIdentity()
    .AddJoinBlobStorage()
    .AddJoinEmailSendingService();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(JoinRpg.IdPortal.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
