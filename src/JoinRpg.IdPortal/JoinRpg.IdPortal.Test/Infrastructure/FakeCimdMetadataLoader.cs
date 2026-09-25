using JoinRpg.IdPortal.OAuthServer.Cimd;

namespace JoinRpg.IdPortal.Test.Infrastructure;

/// <summary>
/// Подменяет сетевую часть CIMD в интеграционных тестах. Ходить по-настоящему некуда:
/// тестовый хост живёт на loopback, а <see cref="CimdAddressGuard"/> справедливо туда не
/// пускает. Сама загрузка и её защиты покрыты юнит-тестами.
/// </summary>
public sealed class FakeCimdMetadataLoader : ICimdMetadataLoader
{
    private readonly Dictionary<string, string> documents = new(StringComparer.Ordinal);

    public int LoadCount { get; private set; }

    /// <summary>Объявить документ, который «лежит» по этому URL.</summary>
    public void Publish(string clientId, string json) => documents[clientId] = json;

    public Task<CimdDocument?> LoadAsync(CimdClientId clientId, CancellationToken ct = default)
    {
        LoadCount++;

        if (!documents.TryGetValue(clientId.Value, out var json))
        {
            return Task.FromResult<CimdDocument?>(null);
        }

        var result = CimdDocument.Validate(json, clientId);
        return Task.FromResult(result.Document);
    }
}
