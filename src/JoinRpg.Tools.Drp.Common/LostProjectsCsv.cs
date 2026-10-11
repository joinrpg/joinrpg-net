using System.Globalization;

namespace JoinRpg.Tools.Drp.Common;

/// <summary>
/// Контракт CSV со списком проектов, созданных после бэкапа: его пишет RestoreLostProjects (#5479),
/// правит человек, читает инструмент оболочек проектов (#5491). Имена колонок не меняются,
/// формат — <see cref="DrpCsv"/>. Читаются колонки по имени, порядок и лишние колонки не важны.
/// </summary>
public static class LostProjectsCsv
{
    public const string ProjectId = "ProjectId";

    /// <summary>«да» / «нет» — решение после ручной проверки. Оболочки создаются только для «да».</summary>
    public const string Create = "Создавать";

    public const string Name = "Название";
    public const string NameVariants = "ВариантыНазвания";
    public const string CreatorUserId = "СоздательId";
    public const string CreatorEmail = "СоздательEmail";
    public const string CreatorMethod = "СпособОпределения";
    public const string OtherCandidates = "ДругиеКандидаты";
    public const string FirstActivity = "ПерваяАктивность";
    public const string LastActivity = "ПоследняяАктивность";
    public const string Notifications = "Уведомлений";
    public const string LogRequests = "ЗапросовВЛогах";
    public const string LogUsers = "ПользователиВЛогах";
    public const string CreatorInLogs = "СоздательВЛогах";
    public const string KogdaIgra = "КогдаИгра";
    public const string MssqlName = "НазваниеВMssql";
    public const string Flags = "Пометки";

    public const string Yes = "да";
    public const string No = "нет";

    /// <summary>Все колонки в порядке записи.</summary>
    public static readonly IReadOnlyList<string> Columns =
    [
        ProjectId, Create, Name, NameVariants, CreatorUserId, CreatorEmail, CreatorMethod, OtherCandidates,
        FirstActivity, LastActivity, Notifications, LogRequests, LogUsers, CreatorInLogs, KogdaIgra, MssqlName, Flags,
    ];

    /// <summary>
    /// Читает проверенный список. Ошибки (нет колонки, не число, не «да»/«нет») собираются по всему
    /// файлу и бросаются разом — <see cref="DrpCsvException"/>.
    /// </summary>
    public static async Task<IReadOnlyList<LostProjectsCsvRow>> Read(string path, CancellationToken ct)
        => Parse(await DrpCsv.ReadFile(path, ct));

    public static IReadOnlyList<LostProjectsCsvRow> Parse(string text)
    {
        var records = DrpCsv.Parse(text);
        if (records.Count == 0)
        {
            throw new DrpCsvException(["Файл пуст: нет даже строки заголовка."]);
        }

        var header = records[0].Values.Select(h => h.Trim()).ToList();
        var errors = new List<string>();
        int Column(string name)
        {
            var index = header.IndexOf(name);
            if (index < 0)
            {
                errors.Add($"Нет колонки «{name}».");
            }
            return index;
        }

        var projectIdColumn = Column(ProjectId);
        var createColumn = Column(Create);
        var nameColumn = Column(Name);
        var creatorColumn = Column(CreatorUserId);
        if (errors.Count > 0)
        {
            throw new DrpCsvException(errors);
        }

        var rows = new List<LostProjectsCsvRow>();
        foreach (var (line, values) in records.Skip(1))
        {
            string Value(int column) => column < values.Count ? values[column].Trim() : "";
            var prefix = $"Строка {line.ToString(CultureInfo.InvariantCulture)}";

            var idText = Value(projectIdColumn);
            if (!int.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out var projectId) || projectId <= 0)
            {
                errors.Add($"{prefix}: некорректен {ProjectId} «{idText}».");
            }

            var createText = Value(createColumn);
            bool? create = createText.ToLowerInvariant() switch
            {
                Yes => true,
                No => false,
                _ => null,
            };
            if (create is null)
            {
                errors.Add($"{prefix}: в колонке «{Create}» нужно «{Yes}» или «{No}», а не «{createText}».");
            }

            var creatorText = Value(creatorColumn);
            int? creatorUserId = null;
            if (creatorText.Length > 0)
            {
                if (int.TryParse(creatorText, NumberStyles.None, CultureInfo.InvariantCulture, out var creator) && creator > 0)
                {
                    creatorUserId = creator;
                }
                else
                {
                    errors.Add($"{prefix}: некорректен {CreatorUserId} «{creatorText}».");
                }
            }

            var name = DrpCsv.FromUserText(Value(nameColumn));
            rows.Add(new LostProjectsCsvRow(line, projectId, create ?? false, string.IsNullOrEmpty(name) ? null : name, creatorUserId));
        }

        if (errors.Count > 0)
        {
            throw new DrpCsvException(errors);
        }
        return rows;
    }
}

/// <summary>
/// Строка проверенного списка — то, что нужно для создания оболочки.
/// </summary>
/// <param name="Line">Номер строки файла — для сообщений об ошибках.</param>
public sealed record LostProjectsCsvRow(int Line, int ProjectId, bool Create, string? Name, int? CreatorUserId);
