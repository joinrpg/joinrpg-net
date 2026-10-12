using ClosedXML.Excel;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Services.Drp.MasterExports;
using JoinRpg.Services.Export;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Services.Drp.Test.MasterExports;

/// <summary>
/// Ссылки как у <c>UriServiceImpl</c>: абсолютные, персонаж — со слешем на конце (так отдаёт
/// <c>LinkGenerator</c> для маршрута <c>~/{projectId}/character/{characterid}/</c>).
/// </summary>
internal sealed class PortalLikeUriService : IUriService
{
    private const string Base = "https://joinrpg.ru";

    public string Get(ILinkable link) => GetUri(link).ToString();

    public Uri GetUri(ILinkable link) => link.LinkType switch
    {
        LinkType.ResultCharacter => new($"{Base}/{link.ProjectId}/character/{link.Identification}/"),
        LinkType.ResultCharacterGroup => new($"{Base}/{link.ProjectId}/roles/{link.Identification}/details"),
        LinkType.Claim => new($"{Base}/{link.ProjectId}/claim/{link.Identification}/edit"),
        _ => throw new ArgumentOutOfRangeException(nameof(link)),
    };
}

internal static class MasterExportTestHelpers
{
    private static readonly IExportDataService ExportService =
        new ServiceCollection().AddJoinExportService().BuildServiceProvider().GetRequiredService<IExportDataService>();

    /// <summary>Выгрузка настоящим бэкендом <c>ClosedXmlExcelBackend</c>.</summary>
    public static byte[] ExportXlsx<T>(IEnumerable<T> rows, IGeneratorFrontend<T> frontend) where T : class
        => ExportService.GetGenerator(ExportType.ExcelXml, rows, frontend).Generate();

    /// <summary>Строка заголовков xlsx — страж тестов, что модули проекта действительно добавили свои колонки.</summary>
    public static string[] XlsxHeaders(byte[] xlsx)
    {
        using var stream = new MemoryStream(xlsx);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var lastColumn = sheet.LastColumnUsed()!.ColumnNumber();
        return [.. Enumerable.Range(1, lastColumn).Select(c => sheet.Cell(1, c).GetString())];
    }

    public static ParsedMasterExport Parse(byte[] bytes, string fileName = "export")
    {
        using var stream = new MemoryStream(bytes);
        return MasterExportParser.Parse(stream, fileName);
    }

    /// <summary>xlsx «руками»: строки как есть, значения — строки, числа или даты.</summary>
    public static byte[] Xlsx(params object?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");
        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                var cell = sheet.Cell(r + 1, c + 1);
                _ = rows[r][c] switch
                {
                    null => cell.SetValue(Blank.Value),
                    int i => cell.SetValue(i),
                    DateTime d => cell.SetValue(d),
                    Hyperlinked h => cell.SetValue(h.Text),
                    var other => cell.SetValue(other.ToString()),
                };
                if (rows[r][c] is Hyperlinked hyperlinked)
                {
                    cell.GetHyperlink().ExternalAddress = new Uri(hyperlinked.Url);
                }
            }
        }
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static string Other(this MasterExportRow row, string header) => row.OtherCells.Single(c => c.Header == header).Value;

    /// <summary>
    /// Заголовки нынешней выгрузки заявок без модулей (заезд, льготы, поселение, паспорт) — до полей.
    /// <paramref name="archived"/> — от игрока остаётся одна колонка.
    /// </summary>
    public static string[] ClaimHeaders(bool archived = false) =>
    [
        "Имя", "", "Статус", "Причина отказа", "Обновлена", "Создана", "Итого взнос", "Осталось", "Уплачено",
        "LastModifiedBy.DisplayName", "Ответственный.DisplayName",
        .. archived
            ? new[] { "Игрок" }
            : ["Игрок", "Игрок.Фамилия", "Игрок.Отчество", "Игрок.Имя", "Игрок.Email", "ВК", "Телеграм", "Игрок.Livejournal", "Игрок.Телефон"],
    ];

    /// <summary>Строка заявки под <see cref="ClaimHeaders"/>: имя, ссылка, статус, остальное пусто, затем поля.</summary>
    public static object?[] ClaimRow(int claimId, string status, bool archived = false, params object?[] fields)
    {
        var row = new object?[ClaimHeaders(archived).Length];
        row[0] = "Арагорн";
        row[1] = $"https://joinrpg.ru/12/claim/{claimId}/edit";
        row[2] = status;
        row[6] = 1500;
        return [.. row, .. fields];
    }
}

/// <summary>Ячейка xlsx с текстом и гиперссылкой.</summary>
internal record Hyperlinked(string Text, string Url);
