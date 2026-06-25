namespace JoinRpg.IdPortal.OAuthServer;

/// <summary>
/// Service for managing OpenID Connect client registrations.
/// </summary>
public interface IOAuthClientService
{
    /// <summary>
    /// Creates a new confidential OAuth client with a randomly generated secret.
    /// </summary>
    /// <param name="clientId">Unique client identifier (max 100 characters).</param>
    /// <param name="displayName">Human-readable name shown in consent screens. Can be <c>null</c>.</param>
    /// <param name="redirectUris">Allowed redirect URIs for the authorization code flow. Must contain at least one entry.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated client secret. Shown only once — it is stored hashed and cannot be retrieved later.</returns>
    Task<string> CreateClientAsync(string clientId, string? displayName, IReadOnlyList<Uri> redirectUris, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates an OAuth client using a caller-supplied secret.
    /// If a client with <paramref name="clientId"/> already exists it is updated; otherwise it is created.
    /// </summary>
    /// <param name="clientId">Unique client identifier.</param>
    /// <param name="clientSecret">Client secret to store (will be hashed).</param>
    /// <param name="displayName">Human-readable name. Can be <c>null</c>.</param>
    /// <param name="redirectUris">Allowed redirect URIs.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateOrUpdateClientAsync(string clientId, string clientSecret, string? displayName, IReadOnlyList<Uri> redirectUris, CancellationToken ct = default);

    /// <summary>
    /// Deletes an OAuth client by its unique client identifier.
    /// </summary>
    /// <param name="clientId">Unique client identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when the client is not found.</exception>
    Task DeleteClientAsync(string clientId, CancellationToken ct = default);
}
