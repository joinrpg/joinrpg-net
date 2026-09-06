using System.Security.Cryptography;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.IdPortal.OAuthServer;

internal class OAuthClientService(IOpenIddictApplicationManager manager) : IOAuthClientService
{
    public async Task<string?> CreateClientAsync(
        string clientId,
        string? displayName,
        IReadOnlyList<Uri> redirectUris,
        OAuthClientType clientType,
        IReadOnlyList<string> scopes,
        bool allowRefreshToken,
        CancellationToken ct = default)
    {
        string? secret = null;
        if (clientType == OAuthClientType.Confidential)
        {
            var secretBytes = new byte[32];
            RandomNumberGenerator.Fill(secretBytes);
            secret = Convert.ToBase64String(secretBytes);
        }

        await CreateOrUpdateClientAsync(clientId, secret, displayName, redirectUris, clientType, scopes, allowRefreshToken, ct);
        return secret;
    }

    public async Task CreateOrUpdateClientAsync(
        string clientId,
        string? clientSecret,
        string? displayName,
        IReadOnlyList<Uri> redirectUris,
        OAuthClientType clientType,
        IReadOnlyList<string> scopes,
        bool allowRefreshToken,
        CancellationToken ct = default)
    {
        var descriptor = BuildDescriptor(clientId, clientSecret, displayName, redirectUris, clientType, scopes, allowRefreshToken);

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

    public async Task<string?> UpdateClientAsync(
        string clientId,
        string? displayName,
        IReadOnlyList<Uri> redirectUris,
        OAuthClientType clientType,
        IReadOnlyList<string> scopes,
        bool allowRefreshToken,
        CancellationToken ct = default)
    {
        var existing = await manager.FindByClientIdAsync(clientId, ct)
            ?? throw new InvalidOperationException($"OAuth client with ClientId={clientId} not found.");

        string? newSecret = null;
        string? secretForDescriptor = null;

        if (clientType == OAuthClientType.Confidential)
        {
            // Preserve the existing (already-hashed) secret: UpdateAsync only re-hashes when the
            // descriptor's ClientSecret differs from what's currently stored, so feeding it back
            // unchanged leaves the secret untouched instead of wiping it.
            var current = new OpenIddictApplicationDescriptor();
            await manager.PopulateAsync(current, existing, ct);
            secretForDescriptor = current.ClientSecret;

            // The client never had a secret (e.g. it was public before) — it needs one now.
            if (string.IsNullOrEmpty(secretForDescriptor))
            {
                var secretBytes = new byte[32];
                RandomNumberGenerator.Fill(secretBytes);
                newSecret = Convert.ToBase64String(secretBytes);
                secretForDescriptor = newSecret;
            }
        }

        var descriptor = BuildDescriptor(clientId, secretForDescriptor, displayName, redirectUris, clientType, scopes, allowRefreshToken);
        await manager.UpdateAsync(existing, descriptor, ct);
        return newSecret;
    }

    public async Task<string> RegenerateSecretAsync(string clientId, CancellationToken ct = default)
    {
        var existing = await manager.FindByClientIdAsync(clientId, ct)
            ?? throw new InvalidOperationException($"OAuth client with ClientId={clientId} not found.");

        var clientType = await manager.GetClientTypeAsync(existing, ct);
        if (!string.Equals(clientType, ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"OAuth client with ClientId={clientId} is public and has no secret to regenerate.");
        }

        var secretBytes = new byte[32];
        RandomNumberGenerator.Fill(secretBytes);
        var secret = Convert.ToBase64String(secretBytes);

        await manager.UpdateAsync(existing, secret, ct);
        return secret;
    }

    private static OpenIddictApplicationDescriptor BuildDescriptor(
        string clientId,
        string? clientSecret,
        string? displayName,
        IReadOnlyList<Uri> redirectUris,
        OAuthClientType clientType,
        IReadOnlyList<string> scopes,
        bool allowRefreshToken)
    {
        // Defense in depth: a joinrpg.* client never gets OIDC scopes, even if the caller made a mistake.
        var effectiveScopes = scopes.Any(JoinRpgScopes.IsJoinRpgScope)
            ? scopes.Where(JoinRpgScopes.IsJoinRpgScope)
            : scopes;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = displayName,
            ClientType = clientType == OAuthClientType.Public ? ClientTypes.Public : ClientTypes.Confidential,
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
            }
        };

        if (allowRefreshToken)
        {
            descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OfflineAccess);
        }

        foreach (var scope in effectiveScopes)
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
        }

        foreach (var uri in redirectUris)
        {
            descriptor.RedirectUris.Add(uri);
        }
        return descriptor;
    }
}
