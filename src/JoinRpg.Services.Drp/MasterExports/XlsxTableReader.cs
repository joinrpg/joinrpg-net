using System.Globalization;
using ClosedXML.Excel;

namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>Читает xlsx-выгрузку (<c>ClosedXmlExcelBackend</c>): первый лист, заголовки в первой строке.</summary>
internal static class XlsxTableReader
{
    // Должно совпадать с ClosedXmlExcelBackend.TruncateIfTooLong: обрезанное значение всегда ровно
    // в лимит ячейки Excel и обёрнуто маркерами.
    internal const int ExcelMaxCellLength = 32767;
    internal const string TruncatedPrefix = "(обрезано) ";
    internal const string TruncatedSuffix = "...";

    public static RawTable Read(Stream content, string fileName)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(content);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new MasterExportFormatException(MasterExportFormatError.UnsupportedFile, fileName, ex);
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault();
            var lastRow = sheet?.LastRowUsed()?.RowNumber() ?? 0;
            var lastColumn = sheet?.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (sheet is null || lastRow == 0 || lastColumn == 0)
            {
                throw new MasterExportFormatException(MasterExportFormatError.Empty, fileName);
            }

            var headers = Enumerable.Range(1, lastColumn).Select(c => ReadCell(sheet.Cell(1, c)).Text.Trim()).ToList();

            var rows = new List<RawRow>();
            for (var r = 2; r <= lastRow; r++)
            {
                var row = new RawRow(r, [.. Enumerable.Range(1, lastColumn).Select(c => ReadCell(sheet.Cell(r, c)))]);
                if (!row.IsEmpty)
                {
                    rows.Add(row);
                }
            }

            return new RawTable(headers, rows, GetSuggestedDate(workbook.Properties));
        }
    }

    private static RawCell ReadCell(IXLCell cell)
    {
        var value = cell.Value;
        var result = value.Type switch
        {
            XLDataType.Blank => RawCell.Empty,
            XLDataType.Text => FromText(value.GetText()),
            XLDataType.Number => new RawCell(value.GetNumber().ToString(CultureInfo.InvariantCulture), Number: value.GetNumber()),
            XLDataType.DateTime => new RawCell(value.GetDateTime().ToString(CultureInfo.InvariantCulture), Date: value.GetDateTime()),
            _ => FromText(cell.GetFormattedString()),
        };
        // Мастер мог переписать текст ссылки, оставив саму гиперссылку.
        if (cell.HasHyperlink && cell.GetHyperlink().ExternalAddress is { } address)
        {
            result = result with { Hyperlink = address.IsAbsoluteUri ? address.AbsoluteUri : address.OriginalString };
        }
        return result;
    }

    internal static RawCell FromText(string text)
    {
        if (text.Length == ExcelMaxCellLength
            && text.StartsWith(TruncatedPrefix, StringComparison.Ordinal)
            && text.EndsWith(TruncatedSuffix, StringComparison.Ordinal))
        {
            return new RawCell(text[TruncatedPrefix.Length..^TruncatedSuffix.Length], Truncated: true);
        }
        return new RawCell(text);
    }

    /// <summary>
    /// Когда книгу создали (ClosedXML ставит это при выгрузке), иначе когда сохранили последний раз.
    /// Без пояса считаем UTC.
    /// </summary>
    private static DateTimeOffset? GetSuggestedDate(XLWorkbookProperties properties)
    {
        foreach (var date in new DateTime?[] { properties.Created, properties.Modified })
        {
            if (date is { } value && value != default)
            {
                return value.Kind == DateTimeKind.Local
                    ? new DateTimeOffset(value).ToUniversalTime()
                    : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
            }
        }
        return null;
    }
}
