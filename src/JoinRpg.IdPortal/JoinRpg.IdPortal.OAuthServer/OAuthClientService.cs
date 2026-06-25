using System.Security.Cryptography;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.IdPortal.OAuthServer;

internal class OAuthClientService(IOpenIddictApplicationManager manager) : IOAuthClientService
{
    public async Task<string> CreateClientAsync(string clientId, string? displayName, IReadOnlyList<Uri> redirectUris, CancellationToken ct = default)
    {
        var secretBytes = new byte[32];
        RandomNumberGenerator.Fill(secretBytes);
        var secret = Convert.ToBase64String(secretBytes);

        await CreateOrUpdateClientAsync(clientId, secret, displayName, redirectUris, ct);
        return secret;
    }

    public async Task CreateOrUpdateClientAsync(string clientId, string clientSecret, string? displayName, IReadOnlyList<Uri> redirectUris, CancellationToken ct = default)
    {
        var descriptor = BuildDescriptor(clientId, clientSecret, displayName, redirectUris);

        var existing = await manager.FindByClientIdAsync(clientId, ct);
        if (existing is null)
        {
            await manager.CreateAsync(descriptor, ct);
        }
        else
        {
            await manager.UpdateAsync(existing, descriptor, ct);
        }
    }

    public async Task DeleteClientAsync(string clientId, CancellationToken ct = default)
    {
        var application = await manager.FindByClientIdAsync(clientId, ct)
            ?? throw new InvalidOperationException($"OAuth client with ClientId={clientId} not found.");
        await manager.DeleteAsync(application, ct);
    }

    private static OpenIddictApplicationDescriptor BuildDescriptor(string clientId, string clientSecret, string? displayName, IReadOnlyList<Uri> redirectUris)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = displayName,
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
        foreach (var uri in redirectUris)
        {
            descriptor.RedirectUris.Add(uri);
        }
        return descriptor;
    }
}
