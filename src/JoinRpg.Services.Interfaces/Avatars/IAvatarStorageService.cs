namespace JoinRpg.Services.Interfaces.Avatars;

/// <summary>
/// Service that stores avatars to permanent storage
/// </summary>
public interface IAvatarStorageService
{
    /// <summary>
    /// Download avatar from remote URI and save to permanent storage
    /// </summary>
    /// <param name="remoteUri"></param>
    /// <param name="ct"></param>
    /// <returns>Key of the object in permanent storage. Public address is built by <see cref="IAvatarUriResolver"/></returns>
    Task<string?> StoreAvatar(Uri remoteUri, CancellationToken ct = default);
}
