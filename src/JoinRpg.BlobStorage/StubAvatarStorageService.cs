namespace JoinRpg.BlobStorage;

internal class StubAvatarStorageService(ILogger<StubAvatarStorageService> logger) : IAvatarStorageService
{
    Task<Uri?> IAvatarStorageService.StoreAvatar(Uri remoteUri, CancellationToken ct)
    {
        logger.LogWarning("Avatar storage not configured. Request to cache avatar ignored");
        return Task.FromResult<Uri?>(null);
    }
}
