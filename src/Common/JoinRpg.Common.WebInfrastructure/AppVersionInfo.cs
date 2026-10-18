using System.Reflection;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Версия запущенного приложения: номер сборки и коммит из InformationalVersion.
/// Только для сервера: в WASM-клиенте входная сборка — клиентская, версия будет не та.
/// </summary>
public record AppVersionInfo(string Version, string? CommitSha)
{
    private static readonly Lazy<AppVersionInfo> current = new(() =>
        Parse(Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion));

    public static AppVersionInfo Current => current.Value;

    public Uri? CommitUrl => CommitSha is null ? null : new Uri($"https://github.com/joinrpg/joinrpg-net/commit/{CommitSha}");

    /// <summary>
    /// Разбирает InformationalVersion вида <c>1.2.3+sha</c>.
    /// </summary>
    public static AppVersionInfo Parse(string? informationalVersion)
    {
        if (informationalVersion is null)
        {
            return new AppVersionInfo("неизвестна", null);
        }

        var parts = informationalVersion.Split('+', 2);
        return new AppVersionInfo(parts[0], parts.Length == 2 ? parts[1] : null);
    }
}
