using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace JoinRpg.IdPortal.OAuthServer;

/// <summary>
/// Контракт между <c>connect/authorize</c> и страницей согласия (ADR012 §4).
/// <para>
/// Само согласие <c>connect/authorize</c> из запроса не принимает: выдать его может только
/// страница согласия, POST-ом с antiforgery-токеном (см. <see cref="IOAuthConsentService"/>).
/// Иначе достаточно было бы заманить залогиненного пользователя по ссылке, чтобы открыть
/// доступ к его проектам без его ведома — клиенты в CIMD регистрируются самозаписью,
/// так что «свой» client_id доступен кому угодно.
/// </para>
/// <para>
/// Через query приходит только отказ: подделать его никому не выгодно — в худшем случае
/// одна попытка авторизации закончится <c>access_denied</c>.
/// </para>
/// </summary>
public static class OAuthConsent
{
    /// <summary>Отказ, который страница согласия передаёт в <c>connect/authorize</c> через query.</summary>
    public const string ConsentParameter = "consent";
    public const string Denied = "denied";

    /// <summary>Имя поля формы с выбранными проектами — по одному на каждый отмеченный чекбокс.</summary>
    public const string ProjectsParameter = "projects";

    /// <summary>Имя поля формы с решением пользователя и его значение «разрешить».</summary>
    public const string DecisionField = "decision";
    public const string Granted = "granted";

    /// <summary>
    /// Claim type carrying the project ids the user granted access to. Access-token-only.
    /// </summary>
    public const string ProjectsClaimType = "projects";

    public static string FormatProjectIds(IEnumerable<int> projectIds) => string.Join(",", projectIds);

    public static IReadOnlyList<int> ParseProjectIds(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToList();
    }

    /// <summary>
    /// Страница согласия — обычная HTML-форма, и каждый отмеченный чекбокс приезжает
    /// отдельным <c>projects=N</c>. Поэтому значений в query может быть несколько,
    /// и каждое из них само по себе может быть списком через запятую.
    /// </summary>
    public static IReadOnlyList<int> ParseProjectIds(StringValues values)
        => [.. values.SelectMany(value => ParseProjectIds(value))];

    internal static void StoreGrantedProjects(Dictionary<string, JsonElement> properties, IReadOnlyCollection<int> projectIds)
    {
        if (projectIds.Count > 0)
        {
            properties[ProjectsClaimType] = JsonSerializer.SerializeToElement(projectIds);
        }
    }

    internal static int[] ReadGrantedProjects(ImmutableDictionary<string, JsonElement> properties)
        => properties.TryGetValue(ProjectsClaimType, out var element)
            ? element.Deserialize<int[]>() ?? []
            : [];
}
