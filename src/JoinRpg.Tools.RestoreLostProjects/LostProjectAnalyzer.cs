using System.Globalization;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;

namespace JoinRpg.Tools.RestoreLostProjects;

/// <summary>
/// Эвристики без БД: какие проекты потеряны, как они назывались, кто их создал.
/// </summary>
/// <param name="existingProjects">Проекты в восстановленной MSSQL: id → название.</param>
/// <param name="existingUsers">Пользователи в MSSQL — чтобы сверить создателя и разрешить email из логов.</param>
/// <param name="virtualUserIds">Робот и пользователь онлайн-платежей — не мастера.</param>
/// <param name="adminUserIds">Получатели уведомлений админам — их действия в проекте не говорят, что они мастера.</param>
/// <param name="backupMaxProjectId">MAX(ProjectId) сразу после восстановления бэкапа.</param>
internal sealed class LostProjectAnalyzer(
    IReadOnlyDictionary<int, string> existingProjects,
    IReadOnlyCollection<ExistingUser> existingUsers,
    IReadOnlySet<int> virtualUserIds,
    IReadOnlySet<int> adminUserIds,
    int backupMaxProjectId)
{
    private readonly Dictionary<int, ExistingUser> usersById = existingUsers.ToDictionary(u => u.UserId);

    private readonly Dictionary<string, int> userIdsByEmail = existingUsers
        .Where(u => !string.IsNullOrWhiteSpace(u.Email))
        .GroupBy(u => u.Email!.Trim(), StringComparer.OrdinalIgnoreCase)
        .ToDictionary(g => g.Key, g => g.Min(u => u.UserId), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Проект попадает в отчёт, если его id больше майского максимума или если его нет в MSSQL
    /// (второе при id не больше максимума — противоречие, которое надо показать).
    /// </summary>
    public bool IsInteresting(int projectId) => projectId > backupMaxProjectId || !existingProjects.ContainsKey(projectId);

    public static int? GetProjectId(string? entityReference)
        => entityReference is not null && ProjectEntityIdParser.TryParseId(entityReference, out var id) ? id.ProjectId.Value : null;

    private static bool ReferencesProject(string entityReference)
        => ProjectEntityIdParser.TryParseId(entityReference, out var id) && id is ProjectIdentification;

    /// <summary>
    /// Значения EntityReference, по которым надо дочитать уведомления.
    /// </summary>
    public IReadOnlyList<string> SelectReferences(IEnumerable<string> entityReferences)
        => [.. entityReferences.Where(r => GetProjectId(r) is int projectId && IsInteresting(projectId))];

    /// <summary>
    /// Id проектов, к которым относится запись лога: поле ProjectId и первый сегмент RequestPath
    /// («/158/character/…»).
    /// </summary>
    public static IEnumerable<int> GetProjectIds(LogEntry entry)
    {
        if (entry.ProjectId is int projectId)
        {
            yield return projectId;
        }
        if (TryGetProjectIdFromPath(entry.RequestPath) is int fromPath && fromPath != entry.ProjectId)
        {
            yield return fromPath;
        }
    }

    internal static int? TryGetProjectIdFromPath(string? requestPath)
    {
        if (string.IsNullOrEmpty(requestPath))
        {
            return null;
        }
        var segment = requestPath.AsSpan().TrimStart('/');
        var end = segment.IndexOfAny('/', '?');
        if (end >= 0)
        {
            segment = segment[..end];
        }
        return int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 ? id : null;
    }

    /// <param name="notifications">Уведомления по ссылкам из <see cref="SelectReferences"/>.</param>
    /// <param name="logs">Записи логов или null, если логи не переданы.</param>
    public IReadOnlyList<LostProjectReport> Analyze(IReadOnlyCollection<ProjectNotification> notifications, IReadOnlyCollection<LogEntry>? logs)
    {
        var notificationsByProject = notifications
            .Select(n => (ProjectId: GetProjectId(n.EntityReference), Notification: n))
            .Where(x => x.ProjectId is int id && IsInteresting(id))
            .GroupBy(x => x.ProjectId!.Value, x => x.Notification)
            .ToDictionary(g => g.Key, g => g.ToList());

        var logsByProject = (logs ?? [])
            .SelectMany(e => GetProjectIds(e).Select(id => (ProjectId: id, Entry: e)))
            .Where(x => IsInteresting(x.ProjectId))
            .GroupBy(x => x.ProjectId, x => x.Entry)
            .ToDictionary(g => g.Key, g => g.ToList());

        return [.. notificationsByProject.Keys.Union(logsByProject.Keys)
            .Order()
            .Select(id => AnalyzeProject(
                id,
                notificationsByProject.GetValueOrDefault(id) ?? [],
                logsByProject.GetValueOrDefault(id) ?? [],
                logsPassed: logs is not null))];
    }

    private LostProjectReport AnalyzeProject(int projectId, List<ProjectNotification> notifications, List<LogEntry> logs, bool logsPassed)
    {
        var flags = ProjectFlags.None;
        var mssqlName = existingProjects.GetValueOrDefault(projectId);
        if (mssqlName is not null)
        {
            flags |= ProjectFlags.AlreadyInMssql;
        }
        else if (projectId <= backupMaxProjectId)
        {
            flags |= ProjectFlags.IdNotAboveBackupMax;
        }

        var adminNotifications = notifications
            .Where(n => ProjectHeaderParser.TryParseAdminNewProject(n.Header) is not null)
            .OrderBy(n => n.CreatedAt)
            .ToList();

        var (name, variants, nameConflict) = ChooseName(notifications);
        if (name is null)
        {
            flags |= ProjectFlags.NameNotFound;
        }
        if (nameConflict)
        {
            flags |= ProjectFlags.NameConflict;
        }

        var candidates = GetCreatorCandidates(notifications);
        int? creatorId;
        CreatorMethod method;
        IReadOnlyList<CreatorCandidate> otherCandidates;
        if (adminNotifications.Count > 0)
        {
            creatorId = adminNotifications[0].InitiatorUserId;
            method = CreatorMethod.Exact;
            otherCandidates = [];
        }
        else if (candidates.Count > 0)
        {
            creatorId = candidates[0].UserId;
            method = CreatorMethod.Presumed;
            otherCandidates = [.. candidates.Skip(1)];
        }
        else
        {
            creatorId = null;
            method = CreatorMethod.NotFound;
            otherCandidates = [];
            flags |= ProjectFlags.CreatorNotFound;
        }
        if (creatorId is int c && !usersById.ContainsKey(c))
        {
            flags |= ProjectFlags.CreatorMissingInMssql;
        }

        var logUsers = logs
            .Select(e => e.LoggedUser?.Trim())
            .Where(email => !string.IsNullOrEmpty(email))
            .GroupBy(email => email!, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Email: g.Key, Count: g.Count(), UserId: userIdsByEmail.TryGetValue(g.Key, out var id) ? id : (int?)null))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var times = notifications.Select(n => n.CreatedAt)
            .Concat(logs.Select(e => e.Timestamp).OfType<DateTimeOffset>())
            .ToList();

        return new LostProjectReport(
            projectId,
            name,
            variants,
            creatorId,
            creatorId is int cid ? usersById.GetValueOrDefault(cid)?.Email : null,
            method,
            otherCandidates,
            times.Count == 0 ? null : times.Min(),
            times.Count == 0 ? null : times.Max(),
            notifications.Count,
            logs.Count,
            [.. logUsers.Select(u => u.UserId is int id
                ? $"{id.ToString(CultureInfo.InvariantCulture)} ({u.Count.ToString(CultureInfo.InvariantCulture)})"
                : $"{u.Email} ({u.Count.ToString(CultureInfo.InvariantCulture)}, нет в MSSQL)")],
            logsPassed && creatorId is int creator ? logUsers.Any(u => u.UserId == creator) : null,
            adminNotifications.Select(n => ParseKogdaIgra(n.AdminBody)).FirstOrDefault(k => k is not null),
            mssqlName,
            flags);
    }

    /// <summary>
    /// Выбирает название. Уведомление админам о новом проекте — точное название. Иначе — вариант,
    /// который допускает больше всего различных заголовков: название проекта общее для заголовков
    /// разных видов, а хвосты (персонаж, тема) у них разные.
    /// </summary>
    /// <returns>
    /// Название, варианты для отчёта и признак конфликта: несколько названий из уведомлений админам,
    /// ничья между вариантами или заголовок, который выбранное название не объясняет (переименование).
    /// </returns>
    internal static (string? Name, IReadOnlyList<NameVariant> Variants, bool Conflict) ChooseName(IReadOnlyCollection<ProjectNotification> notifications)
    {
        var rowsByName = new Dictionary<string, Dictionary<HeaderKind, int>>(StringComparer.Ordinal);
        var lastSeenByName = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        void AddRows(string name, HeaderKind kind, int count, DateTimeOffset lastSeen)
        {
            if (!lastSeenByName.TryGetValue(name, out var known) || known < lastSeen)
            {
                lastSeenByName[name] = lastSeen;
            }
            if (!rowsByName.TryGetValue(name, out var byKind))
            {
                byKind = [];
                rowsByName[name] = byKind;
            }
            byKind[kind] = byKind.GetValueOrDefault(kind) + count;
        }

        var adminNames = notifications
            .Select(n => (Name: ProjectHeaderParser.TryParseAdminNewProject(n.Header), n.CreatedAt))
            .Where(x => x.Name is not null)
            .GroupBy(x => x.Name!, StringComparer.Ordinal)
            .Select(g => (Name: g.Key, Count: g.Count(), First: g.Min(x => x.CreatedAt), Last: g.Max(x => x.CreatedAt)))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.First)
            .ToList();
        foreach (var (adminName, count, _, last) in adminNames)
        {
            AddRows(adminName, HeaderKind.AdminNewProject, count, last);
        }

        // Одинаковые заголовки (одно событие многим получателям) — одно свидетельство.
        var headerGroups = notifications
            .Where(n => ProjectHeaderParser.TryParseAdminNewProject(n.Header) is null)
            .GroupBy(n => (n.Header, MayBeMassMail: ProjectHeaderParser.MayBeMassMail(n.CreatedAt, ReferencesProject(n.EntityReference))))
            .Select(g => (Candidates: ProjectHeaderParser.GetNameCandidates(g.Key.Header, g.Key.MayBeMassMail), Count: g.Count(), Last: g.Max(n => n.CreatedAt)))
            .Where(g => g.Candidates.Count > 0)
            .ToList();

        var support = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (groupCandidates, count, last) in headerGroups)
        {
            foreach (var (candidate, kind) in groupCandidates)
            {
                AddRows(candidate, kind, count, last);
            }
            foreach (var candidate in groupCandidates.Select(c => c.Name).Distinct(StringComparer.Ordinal))
            {
                support[candidate] = support.GetValueOrDefault(candidate) + 1;
            }
        }

        // Больше заголовков → больше строк → длина. Ничья между несколькими разными заголовками
        // («Дюна: Пробуждение: Пол, …» и «Дюна: Пробуждение: Лето, …») значит, что «: » скорее
        // в названии проекта — берём длинный вариант. Один заголовок ничего не говорит — берём короткий.
        IOrderedEnumerable<string> Rank(IEnumerable<string> names) => names
            .OrderByDescending(n => support.GetValueOrDefault(n))
            .ThenByDescending(n => rowsByName[n].Values.Sum())
            .ThenBy(n => support.GetValueOrDefault(n) > 1 ? -n.Length : n.Length)
            .ThenBy(n => n, StringComparer.Ordinal);

        var topSupport = support.Count == 0 ? 0 : support.Values.Max();
        var ties = support.Where(s => s.Value == topSupport).Select(s => s.Key).ToList();

        string? chosen = adminNames.Count > 0
            ? adminNames[0].Name
            : support.Count == 0 ? null : Rank(ties).First();

        if (chosen is null)
        {
            return (null, [], false);
        }

        var headerBest = headerGroups.Select(g => Rank(g.Candidates.Select(c => c.Name)).First()).ToList();
        var conflict = adminNames.Count > 1
            || (adminNames.Count == 0 && ties.Count > 1)
            || headerGroups.Any(g => !g.Candidates.Any(c => c.Name == chosen));

        var variantNames = new List<string> { chosen };
        variantNames.AddRange(adminNames.Select(a => a.Name));
        if (adminNames.Count == 0)
        {
            variantNames.AddRange(ties);
        }
        variantNames.AddRange(headerBest);

        var variants = variantNames
            .Distinct(StringComparer.Ordinal)
            .Select(n => new NameVariant(n, rowsByName[n], lastSeenByName[n]))
            .OrderBy(v => v.Name == chosen ? 0 : 1)
            .ThenByDescending(v => v.Notifications)
            .ToList();
        return (chosen, variants, conflict);
    }

    /// <summary>
    /// Кто по уведомлениям проекта выступает мастером, самые ранние — первыми. Свидетельства (сверено
    /// с ClaimNotificationTextBuilder, MassProjectEmailService):
    /// инициатор мастерского действия по заявке («Заявка … мастером …»), получатель «**Новая заявка**»,
    /// инициатор публикации вводной и рассылки по проекту. Админы и виртуальные пользователи не считаются.
    /// </summary>
    internal IReadOnlyList<CreatorCandidate> GetCreatorCandidates(IEnumerable<ProjectNotification> notifications)
    {
        IEnumerable<(int UserId, DateTimeOffset At)> Evidence(ProjectNotification n)
        {
            if (ProjectHeaderParser.TryParseAdminNewProject(n.Header) is not null)
            {
                yield break;
            }
            if (n.NewClaim)
            {
                yield return (n.RecipientUserId, n.CreatedAt);
            }
            var initiatorIsMaster = n.MasterInitiated
                || (ProjectEntityIdParser.TryParseId(n.EntityReference, out var id)
                    && id is ProjectIdentification or PlotElementIdentification);
            if (initiatorIsMaster)
            {
                yield return (n.InitiatorUserId, n.CreatedAt);
            }
        }

        return [.. notifications
            .SelectMany(Evidence)
            .Where(e => !adminUserIds.Contains(e.UserId) && !virtualUserIds.Contains(e.UserId))
            .GroupBy(e => e.UserId)
            .Select(g => new CreatorCandidate(g.Key, g.Count(), g.Min(e => e.At)))
            .OrderBy(c => c.FirstSeen)
            .ThenByDescending(c => c.Evidence)
            .ThenBy(c => c.UserId)];
    }

    /// <summary>
    /// Привязка к КогдаИгре из тела уведомления админам (AdminNotificationServiceImpl.DescribeLinkedGame).
    /// </summary>
    internal static KogdaIgraLink? ParseKogdaIgra(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }
        // Первым: в этом варианте дальше идёт свободный текст мастера, ссылки в нём не наши.
        if (body.Contains("игры нет на КогдаИгре", StringComparison.Ordinal))
        {
            return new KogdaIgraLink(OnKogdaIgra: false, null);
        }
        const string gamePath = "/game/";
        var gameIndex = body.IndexOf(gamePath, StringComparison.Ordinal);
        if (gameIndex >= 0)
        {
            var digits = body.AsSpan(gameIndex + gamePath.Length);
            var length = 0;
            while (length < digits.Length && char.IsAsciiDigit(digits[length]))
            {
                length++;
            }
            if (int.TryParse(digits[..length], NumberStyles.None, CultureInfo.InvariantCulture, out var gameId))
            {
                return new KogdaIgraLink(OnKogdaIgra: true, gameId);
            }
        }
        if (body.Contains("игра есть на КогдаИгре", StringComparison.Ordinal))
        {
            return new KogdaIgraLink(OnKogdaIgra: true, null);
        }
        return null;
    }
}
