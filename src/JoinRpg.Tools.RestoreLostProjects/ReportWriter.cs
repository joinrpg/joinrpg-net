using System.Globalization;
using JoinRpg.Tools.Drp.Common;
using static JoinRpg.Tools.Drp.Common.DrpCsv;

namespace JoinRpg.Tools.RestoreLostProjects;

/// <summary>
/// CSV-отчёт, строка на проект. Формат и имена колонок — контракт <see cref="LostProjectsCsv"/>:
/// после ручной проверки отчёт читает инструмент оболочек проектов (#5491). Содержит персональные
/// данные (заголовки уведомлений, email) — хранить только локально.
/// </summary>
internal static class ReportWriter
{
    /// <summary>
    /// С этими пометками оболочку по умолчанию не создаём: проект уже есть или id противоречит бэкапу,
    /// создателя нет, или нет названия (проект только по логам) — вслепую создавать не стоит.
    /// Конфликт названий не мешает: выбранное название человек проверит.
    /// </summary>
    private const ProjectFlags BlockingFlags = ProjectFlags.IdNotAboveBackupMax | ProjectFlags.AlreadyInMssql
        | ProjectFlags.CreatorNotFound | ProjectFlags.CreatorMissingInMssql | ProjectFlags.OnlyInLogs | ProjectFlags.NameNotFound;

    internal static bool CreateByDefault(LostProjectReport p)
        => (p.Flags & BlockingFlags) == 0 && p.CreatorUserId is not null && p.Name is not null;

    public static IEnumerable<IReadOnlyList<string?>> BuildRows(IEnumerable<LostProjectReport> projects)
    {
        yield return LostProjectsCsv.Columns;
        foreach (var p in projects)
        {
            var row = new Dictionary<string, string?>
            {
                [LostProjectsCsv.ProjectId] = Number(p.ProjectId),
                [LostProjectsCsv.Create] = CreateByDefault(p) ? LostProjectsCsv.Yes : LostProjectsCsv.No,
                [LostProjectsCsv.Name] = UserText(p.Name),
                [LostProjectsCsv.NameVariants] = UserText(string.Join(" | ", p.NameVariants.Select(DescribeVariant))),
                [LostProjectsCsv.CreatorUserId] = p.CreatorUserId is int creator ? Number(creator) : null,
                [LostProjectsCsv.CreatorEmail] = UserText(p.CreatorEmail),
                [LostProjectsCsv.CreatorMethod] = DescribeMethod(p.CreatorMethod),
                [LostProjectsCsv.OtherCandidates] = string.Join(", ", p.OtherCreatorCandidates.Select(DescribeCandidate)),
                [LostProjectsCsv.FirstActivity] = Time(p.FirstActivity),
                [LostProjectsCsv.LastActivity] = Time(p.LastActivity),
                [LostProjectsCsv.Notifications] = Number(p.Notifications),
                [LostProjectsCsv.LogRequests] = Number(p.LogRequests),
                [LostProjectsCsv.LogUsers] = UserText(string.Join(", ", p.LogUsers)),
                [LostProjectsCsv.CreatorInLogs] = p.CreatorInLogs switch { true => "да", false => "нет", null => null },
                [LostProjectsCsv.KogdaIgra] = p.KogdaIgra?.ToString(),
                [LostProjectsCsv.MssqlName] = UserText(p.MssqlName),
                [LostProjectsCsv.Flags] = DescribeFlags(p.Flags),
            };
            yield return [.. LostProjectsCsv.Columns.Select(c => row[c])];
        }
    }

    public static string Build(IEnumerable<LostProjectReport> projects) => Format(BuildRows(projects));

    public static Task Write(string path, IEnumerable<LostProjectReport> projects, CancellationToken ct)
        => WriteFile(path, BuildRows(projects), ct);

    internal static string DescribeFlags(ProjectFlags flags)
    {
        var result = new List<string>();
        if (flags.HasFlag(ProjectFlags.OnlyInLogs))
        {
            result.Add("только по логам");
        }
        if (flags.HasFlag(ProjectFlags.NameConflict))
        {
            result.Add("конфликт названий");
        }
        if (flags.HasFlag(ProjectFlags.NameNotFound))
        {
            result.Add("название не найдено");
        }
        if (flags.HasFlag(ProjectFlags.CreatorNotFound))
        {
            result.Add("создатель не найден");
        }
        if (flags.HasFlag(ProjectFlags.CreatorMissingInMssql))
        {
            result.Add("создатель отсутствует в MSSQL");
        }
        if (flags.HasFlag(ProjectFlags.IdNotAboveBackupMax))
        {
            result.Add("id не больше майского максимума, но проекта нет в MSSQL");
        }
        if (flags.HasFlag(ProjectFlags.AlreadyInMssql))
        {
            result.Add("проект с этим id уже есть в MSSQL");
        }
        return string.Join(", ", result);
    }

    private static string DescribeMethod(CreatorMethod method) => method switch
    {
        CreatorMethod.Exact => "точно",
        CreatorMethod.Presumed => "предположительно",
        CreatorMethod.PresumedFromLogs => "предположительно (по логам)",
        CreatorMethod.NotFound => "не найден",
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };

    private static string DescribeVariant(NameVariant variant)
        => variant.Name + " (" + string.Join(", ", variant.NotificationsByKind
            .OrderBy(k => k.Key)
            .Select(k => $"{DescribeKind(k.Key)}: {Number(k.Value)}"))
            + $", последнее {variant.LastSeen.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)})";

    private static string DescribeKind(HeaderKind kind) => kind switch
    {
        HeaderKind.AdminNewProject => "админам о новом проекте",
        HeaderKind.Claim => "заявки",
        HeaderKind.Forum => "форум",
        HeaderKind.Plot => "вводные",
        HeaderKind.Room => "комнаты",
        HeaderKind.Invites => "приглашения",
        HeaderKind.ProjectClosed => "закрытие проекта",
        HeaderKind.MassMail => "рассылки",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string DescribeCandidate(CreatorCandidate c)
        => $"{Number(c.UserId)} ({Number(c.Evidence)} свид., с {c.FirstSeen.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)})";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string? Time(DateTimeOffset? value)
        => value?.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
}
