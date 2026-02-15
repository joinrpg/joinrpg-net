using JoinRpg.BlobStorage;
using JoinRpg.Common.WebInfrastructure;
using JoinRpg.Dal.Impl;
using JoinRpg.Dal.Notifications;
using JoinRpg.IdPortal.Components.Account;
using JoinRpg.Interfaces;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Notifications;
using Microsoft.AspNetCore.Components.Authorization;

namespace JoinRpg.IdPortal;

public static class Startup
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseJoinSerilog("JoinRpg.IdPortal");

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

        builder.Services
            .Configure<JoinRpgHostNamesOptions>(builder.Configuration.GetSection("JoinRpgHostNames"))
            .Configure<S3StorageOptions>(builder.Configuration.GetSection("S3BlobStorage"))
            .Configure<NotificationsOptions>(builder.Configuration.GetSection("Notifications"));

        builder.Services.AddJoinExternalLogins(builder.Configuration.GetSection("Authentication"));

        builder.Services.AddJoinWebPlatform(builder.Configuration,
                    builder.Environment,
                    "JoinRpg.IdPortal",
                    dataProtectionConnectionStringName: "DataProtection",
                    telemetryServiceNames: []);

        builder.Services.AddUserServicesOnly();

        builder.Services
            .AddJoinIdentity()
            .AddJoinBlobStorage()
            .AddJoinNotificationLayerServices()
            .AddNotificationsDal(builder.Configuration, builder.Environment)
            ;

        return builder;
    }
}
