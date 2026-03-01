using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.IdPortal.OAuthServer;

public class OAuthRegistrator(
    IServiceProvider serviceProvider,
    IOptions<OAuthServerOptions> options,
    ILogger<OAuthRegistrator> logger
    ) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {

        using var serviceScope = serviceProvider.CreateScope();

        var manager = serviceScope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var registration in options.Value.Registrations)
        {
            if (await manager.FindByClientIdAsync(registration.ClientId, cancellationToken) is null)
            {
                logger.LogInformation("Creating OAuth client with ClientId={OAuthClientId}", registration.ClientId);
                _ = await manager.CreateAsync(CreateDescriptor(registration), cancellationToken);
            }
        }
    }

    private static OpenIddictApplicationDescriptor CreateDescriptor(OAuthServerOptions.OAuthServerRegistrationOptions registration)
    {
        var desc = new OpenIddictApplicationDescriptor
        {
            ClientId = registration.ClientId,
            ClientSecret = registration.ClientSecret,
            ClientType = ClientTypes.Confidential,
            Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                }
        };
        desc.RedirectUris.Add(registration.RedirectUri);
        return desc;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
