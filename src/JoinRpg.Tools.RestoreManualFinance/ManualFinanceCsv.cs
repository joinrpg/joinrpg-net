using System.Globalization;
using JoinRpg.DomainTypes;
using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreManualFinance;

/// <summary>
/// Контракт CSV плана восстановления ручных финансовых операций: его пишет dry-run (<see cref="ReportWriter"/>),
/// правит человек (только колонку <see cref="PaymentType"/>), читает <c>--apply</c>. Формат — <see cref="DrpCsv"/>.
/// При чтении нужны только <see cref="Project"/>, <see cref="Key"/> и <see cref="PaymentType"/>; порядок и
/// остальные колонки не важны. Строки заявок (без ключа) пропускаются.
/// </summary>
internal static class ManualFinanceCsv
{
    public const string Project = "Проект";
    public const string Claim = "Заявка";

    /// <summary>Ключ строки плана (<see cref="Services.Drp.ManualFinance.FinanceOperationToRestore.Key"/>) — по нему сопоставляется CSV.</summary>
    public const string Key = "Ключ";

    public const string Kind = "Вид";
    public const string Id = "Id";
    public const string Time = "Время (UTC)";
    public const string Initiator = "Инициатор";
    public const string MarkedBy = "Отметил";
    public const string Money = "Сумма";
    public const string State = "Итоговое состояние";
    public const string Link = "Связь модерации";
    public const string Status = "Статус плана";
    public const string Suggested = "Предложенный тип";

    /// <summary>Единственная редактируемая колонка: <c>&lt;PaymentTypeId&gt;</c>, <c>cash:&lt;userId&gt;</c> или пусто.</summary>
    public const string PaymentType = "Тип оплаты";

    public const string Flags = "Пометки";
    public const string Result = "Результат";

    /// <summary>Все колонки в порядке записи.</summary>
    public static readonly IReadOnlyList<string> Columns =
    [
        Project, Claim, Key, Kind, Id, Time, Initiator, MarkedBy, Money, State, Link, Status, Suggested, PaymentType, Flags, Result,
    ];

    /// <summary>
    /// Читает проверенный CSV. Ошибки (нет колонки, не число, неразборчивый тип) собираются по всему файлу и
    /// бросаются разом — <see cref="DrpCsvException"/>. Повтор ключа — не ошибка файла: такие строки не
    /// применяются (<see cref="ApplyBuilder.AmbiguousKey"/>), остальные применяются.
    /// </summary>
    public static async Task<IReadOnlyList<ManualFinanceCsvRow>> Read(string path, CancellationToken ct)
        => Parse(await DrpCsv.ReadFile(path, ct));

    public static IReadOnlyList<ManualFinanceCsvRow> Parse(string text)
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

        var projectColumn = Column(Project);
        var keyColumn = Column(Key);
        var paymentTypeColumn = Column(PaymentType);
        if (errors.Count > 0)
        {
            throw new DrpCsvException(errors);
        }

        var rows = new List<ManualFinanceCsvRow>();
        foreach (var (line, values) in records.Skip(1))
        {
            string Value(int column) => column < values.Count ? values[column].Trim() : "";
            var prefix = $"Строка {line.ToString(CultureInfo.InvariantCulture)}";

            var key = Value(keyColumn);
            if (key.Length == 0)
            {
                // Строка заявки: предупреждения и счётчики, применять нечего.
                continue;
            }

            var projectText = Value(projectColumn);
            if (!int.TryParse(projectText, NumberStyles.None, CultureInfo.InvariantCulture, out var projectId) || projectId <= 0)
            {
                errors.Add($"{prefix}: некорректен {Project} «{projectText}».");
                continue;
            }

            var paymentTypeText = Value(paymentTypeColumn);
            if (!PaymentTypeChoice.TryParse(paymentTypeText, out var choice))
            {
                errors.Add($"{prefix}: в колонке «{PaymentType}» нужно id типа оплаты, {PaymentTypeChoice.CashPrefix}<id мастера> или пусто, а не «{paymentTypeText}».");
                continue;
            }


            rows.Add(new ManualFinanceCsvRow(line, new ProjectIdentification(projectId), key, choice));
        }

        if (errors.Count > 0)
        {
            throw new DrpCsvException(errors);
        }
        return rows;
    }
}

/// <summary>Строка проверенного CSV — то, что из него берётся при применении.</summary>
/// <param name="Line">Номер строки файла — для сообщений.</param>
/// <param name="PaymentType">Тип оплаты; null — пусто.</param>
internal sealed record ManualFinanceCsvRow(int Line, ProjectIdentification ProjectId, string Key, PaymentTypeChoice? PaymentType);
