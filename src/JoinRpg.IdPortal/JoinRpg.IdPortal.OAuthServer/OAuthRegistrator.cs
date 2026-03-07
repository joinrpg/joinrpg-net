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
            var existing = await manager.FindByClientIdAsync(registration.ClientId, cancellationToken);
            if (existing is null)
            {
                logger.LogInformation("Creating OAuth client with ClientId={OAuthClientId}", registration.ClientId);
                _ = await manager.CreateAsync(CreateDescriptor(registration), cancellationToken);
            }
            else
            {
                logger.LogInformation("Updating OAuth client with ClientId={OAuthClientId}", registration.ClientId);
                await manager.UpdateAsync(existing, CreateDescriptor(registration), cancellationToken);
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
                    Permissions.Prefixes.Scope + Scopes.OpenId,
                    Permissions.Prefixes.Scope + Scopes.Email,
                    Permissions.Prefixes.Scope + Scopes.Phone,
                    Permissions.Prefixes.Scope + Scopes.Profile,
                }
        };
        foreach (var uri in registration.RedirectUris)
        {
            desc.RedirectUris.Add(uri);
        }

        return desc;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
