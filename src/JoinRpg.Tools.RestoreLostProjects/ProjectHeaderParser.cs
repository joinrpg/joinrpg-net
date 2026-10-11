namespace JoinRpg.Tools.RestoreLostProjects;

/// <summary>
/// Разбор заголовков уведомлений: какие названия проекта в них могут стоять. Форматы — по коду
/// сервисов уведомлений (AdminNotificationServiceImpl, ClaimNotificationTextBuilder,
/// ForumNotificationService, MassProjectEmailService, AccommodationNotificationTextBuilder,
/// MasterEmailService) с учётом смен формата.
/// </summary>
/// <remarks>
/// В названии проекта может быть «: », поэтому заголовок не режется по первому двоеточию: каждая
/// позиция «: », после которой идёт хвост известного вида, даёт вариант. Какой из вариантов
/// настоящий, решает <see cref="LostProjectAnalyzer"/> по совпадениям между заголовками.
/// </remarks>
internal static class ProjectHeaderParser
{
    public const string AdminHeaderPrefix = "Новый проект «";
    public const string AdminHeaderSuffix = "» — статус КогдаИгры";

    /// <summary>
    /// До 2026-07-07 (#4438) форум и вводные писали в заголовок ProjectName.ToString():
    /// «ProjectName({Проект}): …».
    /// </summary>
    private const string LegacyPrefix = "ProjectName(";
    private const string LegacySeparator = "): ";
    private const string Separator = ": ";

    /// <summary>
    /// С 2026-09-28 (#5057) перед темой массовой рассылки стоит название проекта. Раньше заголовок
    /// был просто темой — двоеточие в ней ничего не говорит о проекте.
    /// </summary>
    public static readonly DateTimeOffset ProjectInMassMailSince = new(2026, 9, 28, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Название из заголовка уведомления админам «Новый проект «{название}» — статус КогдаИгры».
    /// </summary>
    public static string? TryParseAdminNewProject(string header)
    {
        if (header.Length <= AdminHeaderPrefix.Length + AdminHeaderSuffix.Length
            || !header.StartsWith(AdminHeaderPrefix, StringComparison.Ordinal)
            || !header.EndsWith(AdminHeaderSuffix, StringComparison.Ordinal))
        {
            return null;
        }
        return header[AdminHeaderPrefix.Length..^AdminHeaderSuffix.Length];
    }

    /// <summary>
    /// Может ли уведомление быть массовой рассылкой «{Проект}: {тема}»: рассылки ссылаются на сам
    /// проект (ProjectId), а название проекта в заголовке у них только с 2026-09-28.
    /// </summary>
    public static bool MayBeMassMail(DateTimeOffset createdAt, bool referencesProject)
        => referencesProject && createdAt >= ProjectInMassMailSince;

    /// <summary>
    /// Все варианты названия проекта, которые допускает заголовок, — от короткого к длинному.
    /// Уведомление админам сюда не входит: его разбирает <see cref="TryParseAdminNewProject"/>.
    /// </summary>
    /// <param name="mayBeMassMail">См. <see cref="MayBeMassMail"/>. Тема рассылки произвольная, поэтому
    /// как рассылку заголовок читаем, только если хвост ни под один известный вид не подходит.</param>
    public static IReadOnlyList<(string Name, HeaderKind Kind)> GetNameCandidates(string header, bool mayBeMassMail)
    {
        var result = new List<(string Name, HeaderKind Kind)>();
        if (TryParseAdminNewProject(header) is not null)
        {
            return result;
        }

        if (header.StartsWith(LegacyPrefix, StringComparison.Ordinal))
        {
            AddCandidates(result, header, LegacySeparator, LegacyPrefix.Length, mayBeMassMail);
        }
        else
        {
            AddCandidates(result, header, Separator, 0, mayBeMassMail);
        }

        if (result.Any(c => c.Kind != HeaderKind.MassMail))
        {
            _ = result.RemoveAll(c => c.Kind == HeaderKind.MassMail);
        }
        return result;
    }

    private static void AddCandidates(List<(string, HeaderKind)> result, string header, string separator, int nameStart, bool mayBeMassMail)
    {
        for (var i = header.IndexOf(separator, nameStart, StringComparison.Ordinal);
            i >= 0;
            i = header.IndexOf(separator, i + 1, StringComparison.Ordinal))
        {
            var name = header[nameStart..i].Trim();
            if (name.Length == 0)
            {
                continue;
            }
            if (Classify(header[(i + separator.Length)..], mayBeMassMail) is HeaderKind kind)
            {
                result.Add((name, kind));
            }
        }
    }

    /// <summary>
    /// Вид уведомления по тому, что стоит после названия проекта.
    /// </summary>
    private static HeaderKind? Classify(string rest, bool mayBeMassMail)
    {
        if (rest.StartsWith("тема на форуме ", StringComparison.Ordinal))
        {
            return HeaderKind.Forum;
        }
        if (rest == "опубликована вводная")
        {
            return HeaderKind.Plot;
        }
        if (rest == "приглашения к проживанию")
        {
            return HeaderKind.Invites;
        }
        if (rest.StartsWith("комната ", StringComparison.Ordinal))
        {
            return HeaderKind.Room;
        }
        if (rest is "проект закрыт" or "проект будет закрыт из-за неактивности")
        {
            return HeaderKind.ProjectClosed;
        }
        if (rest.Contains(", игрок ", StringComparison.Ordinal))
        {
            return HeaderKind.Claim;
        }
        if (mayBeMassMail && rest.Length > 0)
        {
            return HeaderKind.MassMail;
        }
        return null;
    }
}
