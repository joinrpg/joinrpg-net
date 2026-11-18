using System.Reflection;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Версия запущенного приложения: номер сборки и коммит из InformationalVersion.
/// Только для сервера: в WASM-клиенте входная сборка — клиентская, версия будет не та.
/// </summary>
/// <param name="CommitUrl">
/// Ссылка на коммит в репозитории самого приложения (а не joinrpg-net: пакет подключают и другие
/// приложения). Есть только для репозитория на GitHub.
/// </param>
public record AppVersionInfo(string Version, string? CommitSha, Uri? CommitUrl)
{
    private static readonly Lazy<AppVersionInfo> current = new(() =>
    {
        var assembly = Assembly.GetEntryAssembly();
        return Parse(
            assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            // SourceLink с PublishRepositoryUrl кладёт адрес репозитория в метаданные сборки.
            assembly?.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "RepositoryUrl")?.Value);
    });

    public static AppVersionInfo Current => current.Value;

    /// <summary>
    /// Разбирает InformationalVersion вида <c>1.2.3+sha</c> и строит ссылку на коммит по адресу репозитория.
    /// </summary>
    public static AppVersionInfo Parse(string? informationalVersion, string? repositoryUrl)
    {
        if (informationalVersion is null)
        {
            return new AppVersionInfo("неизвестна", null, null);
        }

        var parts = informationalVersion.Split('+', 2);
        var commitSha = parts.Length == 2 ? parts[1] : null;
        return new AppVersionInfo(parts[0], commitSha, GetCommitUrl(repositoryUrl, commitSha));
    }

    private static Uri? GetCommitUrl(string? repositoryUrl, string? commitSha)
    {
        if (commitSha is null
            || !Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var repository)
            || repository.Scheme != Uri.UriSchemeHttps
            || repository.Host != "github.com")
        {
            return null;
        }

        var path = repository.AbsolutePath.TrimEnd('/');
        if (path.EndsWith(".git", StringComparison.Ordinal))
        {
            path = path[..^".git".Length];
        }
        return new Uri($"https://github.com{path}/commit/{commitSha}");
    }
}
