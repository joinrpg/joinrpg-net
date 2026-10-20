using JoinRpg.DomainTypes.Users;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Адрес аватарки без хранилища: закешированная копия — под example.com, иначе адрес у провайдера.
/// </summary>
public class FakeAvatarUriResolver : IAvatarUriResolver
{
    public Uri GetAvatarUri(string? cachedUri, string originalUri)
        => cachedUri is null ? new Uri(originalUri) : new Uri($"https://cache.example.com/{cachedUri}");
}
