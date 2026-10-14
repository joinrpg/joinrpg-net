using System.Globalization;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>
/// Разбирает Excel-выгрузки, которые мастер скачал из JoinRPG (список персонажей или список заявок),
/// — для восстановления после потери основной БД (DRP, #5529). Без БД. CSV-выгрузку не разбираем.
/// </summary>
/// <remarks>
/// <para>
/// Колонка ссылок (на персонажа или заявку) опознаётся по содержимому — та, где ссылок больше всего и они
/// составляют не меньше 90% непустых ячеек, — и по сигнатуре выгрузки вокруг неё:
/// <list type="bullet">
/// <item>персонажи — у колонки ссылок заголовок «Персонаж», слева колонка «Персонаж» (имя);</item>
/// <item>заявки — у колонки ссылок пустой заголовок, слева «Имя», и на своих местах нашлись «Статус»,
/// «Итого взнос» и колонка игрока.</item>
/// </list>
/// Так отсекаются другие выгрузки со ссылками на заявки (финансовая, «Мои заявки»). Остальные системные
/// колонки — по заголовку на своём месте (<see cref="MasterExportColumns"/>), всё прочее — кандидаты в поля.
/// </para>
/// <para>
/// Пустые строки пропускаются. Непустая строка, в которой в колонке ссылок не ссылка, остаётся в
/// результате без id и с предупреждением <see cref="MasterExportWarning.RowWithoutLink"/>: решать, что с
/// ней делать, — вызывающему.
/// </para>
/// </remarks>
public static class MasterExportParser
{
    /// <summary>
    /// Как статус заявки пишется в выгрузке — копия Display у <c>ClaimStatusView</c> из
    /// <c>JoinRpg.Web.Claims</c>: на UI-библиотеку сервис ссылаться не должен. Совпадение стережёт тест.
    /// </summary>
    internal static IReadOnlyDictionary<ClaimStatus, string> StatusDisplayNames { get; } = new Dictionary<ClaimStatus, string>
    {
        [ClaimStatus.AddedByUser] = "Подана",
        [ClaimStatus.AddedByMaster] = "Предложена",
        [ClaimStatus.Approved] = "Принята",
        [ClaimStatus.DeclinedByUser] = "Отозвана",
        [ClaimStatus.DeclinedByMaster] = "Отклонена",
        [ClaimStatus.Discussed] = "Обсуждается",
        [ClaimStatus.OnHold] = "В листе ожидания",
        [ClaimStatus.CheckedIn] = "Игрок заехал",
    };

    private static readonly Dictionary<string, ClaimStatus> StatusesByDisplayName =
        StatusDisplayNames.ToDictionary(x => x.Value, x => x.Key);

    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private const char NoBreakSpace = (char)0x00A0;
    private const char NarrowNoBreakSpace = (char)0x202F;

    /// <exception cref="MasterExportFormatException">Файл не похож на выгрузку JoinRPG.</exception>
    public static ParsedMasterExport Parse(Stream content, string fileName)
    {
        var table = ReadTable(content, fileName);
        if (table.Rows.Count == 0)
        {
            throw new MasterExportFormatException(MasterExportFormatError.NoRows, fileName);
        }
        return Interpret(table, fileName);
    }

    private static RawTable ReadTable(Stream content, string fileName)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        var bytes = buffer.ToArray();

        // Тип файла — по содержимому: мастер мог переименовать файл. xlsx — это zip; всё остальное
        // (CSV-выгрузку, старый .xls) не разбираем.
        if (!bytes.AsSpan().StartsWith("PK\u0003\u0004"u8))
        {
            throw new MasterExportFormatException(MasterExportFormatError.UnsupportedFile, fileName);
        }
        buffer.Position = 0;
        return XlsxTableReader.Read(buffer, fileName);
    }

    private static ParsedMasterExport Interpret(RawTable table, string fileName)
    {
        var warnings = new List<MasterExportWarning>();
        var headers = table.Headers;

        var (kind, linkColumn, columns) = DetectKind(table, fileName);
        var linkKind = kind == MasterExportKind.Characters ? MasterExportLinkKind.Character : MasterExportLinkKind.Claim;

        var otherColumns = Enumerable.Range(0, headers.Count)
            .Where(i => !columns.ContainsKey(i))
            .ToList();
        var otherHeaders = otherColumns.Select(i => headers[i]).ToList();
        if (otherHeaders.Distinct(StringComparer.Ordinal).Count() != otherHeaders.Count)
        {
            warnings.Add(MasterExportWarning.DuplicateHeaders);
        }

        int? Find(MasterExportColumn column) => columns.Where(c => c.Value == column).Select(c => (int?)c.Key).FirstOrDefault();

        var nameColumn = Find(MasterExportColumn.Name);
        var groupNamesColumn = Find(MasterExportColumn.GroupNames);
        var groupLinksColumn = Find(MasterExportColumn.GroupLinks);
        var responsibleColumn = Find(MasterExportColumn.ResponsibleMaster);
        var emailColumn = Find(MasterExportColumn.PlayerEmail);
        var playerColumn = Find(MasterExportColumn.PlayerDisplayName);
        var statusColumn = Find(MasterExportColumn.ClaimStatus);
        var totalFeeColumn = Find(MasterExportColumn.TotalFee);
        var createdColumn = Find(MasterExportColumn.CreatedAt);
        var updatedColumn = Find(MasterExportColumn.UpdatedAt);

        var projectIds = new List<int>();
        var mainProjectIds = new List<int>();
        var rows = new List<MasterExportRow>();
        // Проекты из ссылок каждой строки — чтобы пометить строки со ссылками в чужой проект.
        var rowProjectIds = new List<List<int>>();

        foreach (var row in table.Rows)
        {
            var rowWarnings = new List<MasterExportWarning>();
            var thisRowProjectIds = new List<int>();
            rowProjectIds.Add(thisRowProjectIds);

            var link = LinkOf(row[linkColumn]);
            if (link?.Kind != linkKind)
            {
                link = null;
                rowWarnings.Add(MasterExportWarning.RowWithoutLink);
            }
            else
            {
                mainProjectIds.Add(link.ProjectId);
                thisRowProjectIds.Add(link.ProjectId);
            }

            var groups = ReadGroups(row, groupLinksColumn, groupNamesColumn, thisRowProjectIds, rowWarnings);
            projectIds.AddRange(thisRowProjectIds);

            string? Text(int? column) => column is { } c && !string.IsNullOrWhiteSpace(row[c].Text) ? row[c].Text.Trim() : null;

            ClaimStatus? status = null;
            if (Text(statusColumn) is { } statusText)
            {
                if (StatusesByDisplayName.TryGetValue(statusText, out var s))
                {
                    status = s;
                }
                else
                {
                    rowWarnings.Add(MasterExportWarning.UnknownClaimStatus);
                }
            }

            var otherCells = otherColumns.Select(c => new MasterExportCell(headers[c], row[c].Text, row[c].Truncated)).ToList();
            if (otherCells.Any(c => c.Truncated))
            {
                rowWarnings.Add(MasterExportWarning.TruncatedValues);
            }

            rows.Add(new MasterExportRow(
                row.RowNumber,
                CharacterId: link?.Kind == MasterExportLinkKind.Character ? link.EntityId : null,
                ClaimId: link?.Kind == MasterExportLinkKind.Claim ? link.EntityId : null,
                Name: Text(nameColumn),
                groups,
                ResponsibleMasterName: Text(responsibleColumn),
                PlayerEmail: Text(emailColumn),
                PlayerDisplayName: Text(playerColumn),
                status,
                TotalFee: ReadInt(row, totalFeeColumn, rowWarnings),
                ClaimCreatedAt: ReadDate(row, createdColumn, rowWarnings),
                ClaimUpdatedAt: ReadDate(row, updatedColumn, rowWarnings),
                otherCells)
            {
                Warnings = rowWarnings,
            });
        }

        // Один персонаж (заявка) в нескольких строках — помечаем все такие строки.
        var duplicateIds = rows
            .Select(r => r.CharacterId ?? r.ClaimId)
            .Where(id => id is not null)
            .GroupBy(id => id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();
        var projectId = mainProjectIds
            .GroupBy(id => id)
            .OrderByDescending(g => g.Count())
            .First().Key;

        rows = [.. rows.Select((r, i) =>
        {
            IEnumerable<MasterExportWarning> rowWarnings = r.Warnings;
            if (duplicateIds.Contains(r.CharacterId ?? r.ClaimId))
            {
                rowWarnings = rowWarnings.Append(MasterExportWarning.DuplicateEntityId);
            }
            if (rowProjectIds[i].Any(id => id != projectId))
            {
                rowWarnings = rowWarnings.Append(MasterExportWarning.ProjectIdMismatch);
            }
            return r with { Warnings = [.. rowWarnings.Distinct()] };
        })];

        if (projectIds.Distinct().Count() > 1)
        {
            warnings.Add(MasterExportWarning.ProjectIdMismatch);
        }
        warnings.AddRange(rows.SelectMany(r => r.Warnings));

        return new ParsedMasterExport(
            kind,
            projectId,
            table.SuggestedDate,
            otherHeaders,
            rows,
            [.. warnings.Distinct()]);
    }

    /// <summary>Ссылка из текста ячейки, а если текст не ссылка — из гиперссылки xlsx.</summary>
    private static MasterExportLink? LinkOf(RawCell cell)
        => MasterExportLinks.TryParse(cell.Text) ?? (cell.Hyperlink is { } hyperlink ? MasterExportLinks.TryParse(hyperlink) : null);

    /// <summary>Тип выгрузки — по колонке ссылок и сигнатуре вокруг неё (см. remarks класса).</summary>
    private static (MasterExportKind Kind, int LinkColumn, Dictionary<int, MasterExportColumn> Columns) DetectKind(
        RawTable table, string fileName)
    {
        var headers = table.Headers;
        bool LeftIs(int column, string header) => column > 0 && headers[column - 1] == header;

        (MasterExportKind, int, Dictionary<int, MasterExportColumn>)? characters = null;
        if (FindLinkColumn(table, MasterExportLinkKind.Character, c => headers[c] == "Персонаж" && LeftIs(c, "Персонаж")) is { } characterColumn)
        {
            characters = (MasterExportKind.Characters, characterColumn, MasterExportColumns.Assign(MasterExportKind.Characters, headers, characterColumn));
        }

        (MasterExportKind, int, Dictionary<int, MasterExportColumn>)? claims = null;
        if (FindLinkColumn(table, MasterExportLinkKind.Claim, c => headers[c].Length == 0 && LeftIs(c, "Имя")) is { } claimColumn)
        {
            var columns = MasterExportColumns.Assign(MasterExportKind.Claims, headers, claimColumn);
            MasterExportColumn[] required = [MasterExportColumn.ClaimStatus, MasterExportColumn.TotalFee, MasterExportColumn.PlayerDisplayName];
            if (required.All(columns.ContainsValue))
            {
                claims = (MasterExportKind.Claims, claimColumn, columns);
            }
        }

        switch (characters, claims)
        {
            case ({ } result, null):
                return result;
            case (null, { } result):
                return result;
            case ({ }, { }):
                throw new MasterExportFormatException(MasterExportFormatError.AmbiguousKind, fileName);
        }

        var anyLinks = Enumerable.Range(0, headers.Count).Any(c => table.Rows.Any(r =>
            LinkOf(r[c])?.Kind is MasterExportLinkKind.Character or MasterExportLinkKind.Claim));
        throw new MasterExportFormatException(
            anyLinks ? MasterExportFormatError.UnknownExportType : MasterExportFormatError.LinkColumnNotFound,
            fileName);
    }

    /// <summary>
    /// Колонка, где ссылок нужного вида больше всего и они составляют не меньше 90% непустых ячеек:
    /// мастер мог вписать в неё что-то руками.
    /// </summary>
    private static int? FindLinkColumn(RawTable table, MasterExportLinkKind kind, Func<int, bool> headerFits)
    {
        int? best = null;
        var bestCount = 0;
        for (var column = 0; column < table.Headers.Count; column++)
        {
            if (!headerFits(column))
            {
                continue;
            }
            var nonEmpty = table.Rows.Count(r => !r[column].IsEmpty);
            var links = table.Rows.Count(r => LinkOf(r[column])?.Kind == kind);
            if (links > bestCount && links * 10 >= nonEmpty * 9)
            {
                best = column;
                bestCount = links;
            }
        }
        return best;
    }

    /// <summary>
    /// Ссылки на группы и их имена лежат в двух соседних колонках через « | » в одном порядке —
    /// сопоставляем по индексу. Если число не сошлось (« | » в имени группы), имена не угадываем.
    /// </summary>
    private static IReadOnlyList<MasterExportGroupRef> ReadGroups(
        RawRow row, int? linksColumn, int? namesColumn, List<int> projectIds, List<MasterExportWarning> rowWarnings)
    {
        if (linksColumn is not { } lc)
        {
            return [];
        }
        if (MasterExportLinks.TryParseGroupList(row[lc].Text) is not { } links)
        {
            rowWarnings.Add(MasterExportWarning.InvalidValue);
            return [];
        }
        if (links.Count == 0)
        {
            return [];
        }
        projectIds.AddRange(links.Select(l => l.ProjectId));

        var namesText = namesColumn is { } nc ? row[nc].Text : "";
        var names = string.IsNullOrEmpty(namesText) ? [] : namesText.Split(MasterExportLinks.ListSeparator);
        if (names.Length != links.Count)
        {
            rowWarnings.Add(MasterExportWarning.GroupsCountMismatch);
            return [.. links.Select(l => new MasterExportGroupRef(l.EntityId, ""))];
        }
        return [.. links.Select((l, i) => new MasterExportGroupRef(l.EntityId, names[i].Trim()))];
    }

    private static int? ReadInt(RawRow row, int? column, List<MasterExportWarning> rowWarnings)
    {
        if (column is not { } c || row[c].IsEmpty)
        {
            return null;
        }
        var cell = row[c];
        if (cell.Number is { } number && number == Math.Floor(number) && number is >= int.MinValue and <= int.MaxValue)
        {
            return (int)number;
        }
        // Пересохранённый Excel мог разбить тысячи пробелами (в том числе неразрывными).
        var text = cell.Text.Replace(" ", "").Replace(NoBreakSpace.ToString(), "").Replace(NarrowNoBreakSpace.ToString(), "");
        if (int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }
        rowWarnings.Add(MasterExportWarning.InvalidValue);
        return null;
    }

    /// <summary>
    /// Даты в выгрузке — UTC из БД без пояса, нативные даты xlsx. Если мастер пересохранил книгу и дата
    /// стала числом или текстом (в культуре ru-RU) — разбираем и это.
    /// </summary>
    private static DateTimeOffset? ReadDate(RawRow row, int? column, List<MasterExportWarning> rowWarnings)
    {
        if (column is not { } c || row[c].IsEmpty)
        {
            return null;
        }
        var cell = row[c];
        DateTime? date = cell.Date;
        if (date is null && cell.Number is { } number)
        {
            try
            {
                date = DateTime.FromOADate(number);
            }
            catch (ArgumentException)
            {
                date = null;
            }
        }
        if (date is { } nativeDate)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(nativeDate, DateTimeKind.Utc));
        }

        foreach (var culture in new[] { RussianCulture, CultureInfo.InvariantCulture })
        {
            if (DateTime.TryParse(cell.Text.Trim(), culture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                return new DateTimeOffset(parsed);
            }
        }
        rowWarnings.Add(MasterExportWarning.InvalidValue);
        return null;
    }
}
