using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        var clientService = serviceScope.ServiceProvider.GetRequiredService<IOAuthClientService>();

        foreach (var registration in options.Value.Registrations)
        {
            logger.LogInformation("Registering OAuth client with ClientId={OAuthClientId}", registration.ClientId);
            await clientService.CreateOrUpdateClientAsync(
                registration.ClientId,
                registration.ClientSecret,
                registration.DisplayName,
                registration.RedirectUris,
                cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
