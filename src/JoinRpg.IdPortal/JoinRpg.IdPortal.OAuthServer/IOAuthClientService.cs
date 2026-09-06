namespace JoinRpg.IdPortal.OAuthServer;

/// <summary>
/// Service for managing OpenID Connect client registrations.
/// </summary>
public interface IOAuthClientService
{
    /// <summary>
    /// Creates a new OAuth client. For a <see cref="OAuthClientType.Confidential"/> client a secret
    /// is generated; for a <see cref="OAuthClientType.Public"/> client no secret is created (the
    /// client is expected to rely on PKCE instead).
    /// </summary>
    /// <param name="clientId">Unique client identifier (max 100 characters).</param>
    /// <param name="displayName">Human-readable name shown in consent screens. Can be <c>null</c>.</param>
    /// <param name="redirectUris">Allowed redirect URIs for the authorization code flow. Must contain at least one entry.</param>
    /// <param name="clientType">Whether the client can hold a secret securely.</param>
    /// <param name="scopes">
    /// Scopes the client is allowed to request. Must be either standard OIDC scopes or
    /// <see cref="JoinRpgScopes"/> — mixing both is rejected by the caller before this is invoked,
    /// but as defense in depth any OIDC scope is silently dropped whenever a <c>joinrpg.*</c> scope
    /// is present.
    /// </param>
    /// <param name="allowRefreshToken">Whether the client may use the refresh token grant (<c>offline_access</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated client secret for confidential clients, or <c>null</c> for public clients. Shown only once — it is stored hashed and cannot be retrieved later.</returns>
    Task<string?> CreateClientAsync(
        string clientId,
        string? displayName,
        IReadOnlyList<Uri> redirectUris,
        OAuthClientType clientType,
        IReadOnlyList<string> scopes,
        bool allowRefreshToken,
        CancellationToken ct = default);

    /// <summary>
    /// Creates or updates an OAuth client using a caller-supplied secret.
    /// If a client with <paramref name="clientId"/> already exists it is updated; otherwise it is created.
    /// </summary>
    /// <param name="clientId">Unique client identifier.</param>
    /// <param name="clientSecret">Client secret to store (will be hashed), or <c>null</c> for a public client.</param>
    /// <param name="displayName">Human-readable name. Can be <c>null</c>.</param>
    /// <param name="redirectUris">Allowed redirect URIs.</param>
    /// <param name="clientType">Whether the client can hold a secret securely.</param>
    /// <param name="scopes">Scopes the client is allowed to request — see <see cref="CreateClientAsync"/>.</param>
    /// <param name="allowRefreshToken">Whether the client may use the refresh token grant (<c>offline_access</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateOrUpdateClientAsync(
        string clientId,
        string? clientSecret,
        string? displayName,
        IReadOnlyList<Uri> redirectUris,
        OAuthClientType clientType,
        IReadOnlyList<string> scopes,
        bool allowRefreshToken,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes an OAuth client by its unique client identifier.
    /// </summary>
    /// <param name="clientId">Unique client identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when the client is not found.</exception>
    Task DeleteClientAsync(string clientId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing OAuth client's metadata (display name, redirect URIs, client type,
    /// scopes, refresh token support) while leaving its client secret untouched. Use
    /// <see cref="RegenerateSecretAsync"/> to rotate the secret separately.
    /// </summary>
    /// <param name="clientId">Unique client identifier of the client to update.</param>
    /// <param name="displayName">Human-readable name shown in consent screens. Can be <c>null</c>.</param>
    /// <param name="redirectUris">Allowed redirect URIs for the authorization code flow. Must contain at least one entry.</param>
    /// <param name="clientType">Whether the client can hold a secret securely.</param>
    /// <param name="scopes">Scopes the client is allowed to request — see <see cref="CreateClientAsync"/>.</param>
    /// <param name="allowRefreshToken">Whether the client may use the refresh token grant (<c>offline_access</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The newly generated secret when the client is confidential and did not previously have one
    /// (e.g. it is being switched from public to confidential); otherwise <c>null</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the client is not found.</exception>
    Task<string?> UpdateClientAsync(
        string clientId,
        string? displayName,
        IReadOnlyList<Uri> redirectUris,
        OAuthClientType clientType,
        IReadOnlyList<string> scopes,
        bool allowRefreshToken,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a new client secret for an existing confidential OAuth client, invalidating the old one.
    /// </summary>
    /// <param name="clientId">Unique client identifier of the client to rotate the secret for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly generated secret. Shown only once — it is stored hashed and cannot be retrieved later.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the client is not found or is not confidential.</exception>
    Task<string> RegenerateSecretAsync(string clientId, CancellationToken ct = default);
}
