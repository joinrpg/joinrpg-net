using System.Globalization;

namespace JoinRpg.Services.Drp.MasterExports;

internal enum MasterExportLinkKind
{
    Character,
    Claim,
    CharacterGroup,
}

internal record MasterExportLink(MasterExportLinkKind Kind, int ProjectId, int EntityId);

/// <summary>
/// Разбор ссылок, которые выгрузка пишет через <c>UriServiceImpl</c>: абсолютный URL, домен любой.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>персонаж — <c>/{p}/character/{id}</c>, бывает со слешем на конце или с <c>/details</c>;</item>
/// <item>заявка — <c>/{p}/claim/{id}/edit</c>;</item>
/// <item>группа — <c>/{p}/roles/{id}/details</c> (или без <c>/details</c>).</item>
/// </list>
/// </remarks>
internal static class MasterExportLinks
{
    /// <summary>Разделитель списков в ячейке (<c>CustomExporter.UriListColumn</c>, <c>StringListColumn</c>).</summary>
    internal const string ListSeparator = " | ";

    public static MasterExportLink? TryParse(string text)
    {
        var path = GetPath(text.Trim());
        if (path is null)
        {
            return null;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length is < 3 or > 4
            || !TryParseId(segments[0], out var projectId)
            || !TryParseId(segments[2], out var entityId))
        {
            return null;
        }

        var action = segments.Length == 4 ? segments[3] : null;
        return segments[1].ToLowerInvariant() switch
        {
            "character" when action is null || IsAction(action, "details") => new(MasterExportLinkKind.Character, projectId, entityId),
            "claim" when action is not null && IsAction(action, "edit") => new(MasterExportLinkKind.Claim, projectId, entityId),
            "roles" when action is null || IsAction(action, "details") => new(MasterExportLinkKind.CharacterGroup, projectId, entityId),
            _ => null,
        };
    }

    /// <summary>
    /// Список ссылок на группы через « | ». <c>null</c>, если хоть одна часть — не ссылка на группу.
    /// Пустая ячейка — пустой список.
    /// </summary>
    public static IReadOnlyList<MasterExportLink>? TryParseGroupList(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }
        var result = new List<MasterExportLink>();
        foreach (var part in text.Split(ListSeparator))
        {
            if (TryParse(part) is not { Kind: MasterExportLinkKind.CharacterGroup } link)
            {
                return null;
            }
            result.Add(link);
        }
        return result;
    }

    private static string? GetPath(string text)
    {
        if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            return uri.AbsolutePath;
        }
        // На всякий случай — относительная ссылка от корня сайта. На Linux Uri считает её абсолютной
        // (file://), поэтому проверяем отдельно, а не через UriKind.Relative.
        return text.StartsWith('/') && !text.StartsWith("//", StringComparison.Ordinal) ? text.Split('?', '#')[0] : null;
    }

    private static bool IsAction(string segment, string action) => string.Equals(segment, action, StringComparison.OrdinalIgnoreCase);

    private static bool TryParseId(string segment, out int id)
        => int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id >= 0;
}
