using System.Globalization;
using System.Text;

namespace JoinRpg.Tools.RestoreLostProjects;

/// <summary>
/// CSV-отчёт, строка на проект. Содержит персональные данные (заголовки уведомлений, email) — хранить
/// только локально. Разделитель «;» и BOM — чтобы Excel с русской локалью открыл файл без мастера импорта.
/// </summary>
internal static class ReportWriter
{
    private static readonly string[] Header =
    [
        "ProjectId", "Название", "ВариантыНазвания", "СоздательId", "СоздательEmail", "СпособОпределения",
        "ДругиеКандидаты", "ПерваяАктивность", "ПоследняяАктивность", "Уведомлений", "ЗапросовВЛогах",
        "ПользователиВЛогах", "СоздательВЛогах", "КогдаИгра", "НазваниеВMssql", "Пометки",
    ];

    public static string Build(IEnumerable<LostProjectReport> projects)
    {
        var sb = new StringBuilder();
        AppendRow(sb, Header);
        foreach (var p in projects)
        {
            AppendRow(sb,
            [
                Number(p.ProjectId),
                UserText(p.Name),
                UserText(string.Join(" | ", p.NameVariants.Select(DescribeVariant))),
                p.CreatorUserId is int creator ? Number(creator) : null,
                UserText(p.CreatorEmail),
                DescribeMethod(p.CreatorMethod),
                string.Join(", ", p.OtherCreatorCandidates.Select(DescribeCandidate)),
                Time(p.FirstActivity),
                Time(p.LastActivity),
                Number(p.Notifications),
                Number(p.LogRequests),
                UserText(string.Join(", ", p.LogUsers)),
                p.CreatorInLogs switch { true => "да", false => "нет", null => null },
                p.KogdaIgra?.ToString(),
                UserText(p.MssqlName),
                DescribeFlags(p.Flags),
            ]);
        }
        return sb.ToString();
    }

    public static Task Write(string path, IEnumerable<LostProjectReport> projects, CancellationToken ct)
        => File.WriteAllTextAsync(path, Build(projects), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), ct);

    internal static string DescribeFlags(ProjectFlags flags)
    {
        var result = new List<string>();
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

    private static void AppendRow(StringBuilder sb, string?[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                _ = sb.Append(';');
            }
            _ = sb.Append(Escape(values[i]));
        }
        _ = sb.Append("\r\n");
    }

    /// <summary>
    /// Текст, который ввёл пользователь (название, адрес): Excel исполняет ячейку, начинающуюся с =, +, -, @
    /// (а также с табуляции или перевода строки перед ними), как формулу, поэтому такие значения
    /// экранируем апострофом.
    /// </summary>
    internal static string? UserText(string? value)
        => value is { Length: > 0 } && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;

    internal static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }
        return value.IndexOfAny([';', '"', '\r', '\n']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
