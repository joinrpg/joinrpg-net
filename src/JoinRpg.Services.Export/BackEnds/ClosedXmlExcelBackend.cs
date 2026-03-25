using System.Text.RegularExpressions;
using ClosedXML.Excel;
using JoinRpg.Services.Export.Internal;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.BackEnds;

internal partial class ClosedXmlExcelBackend : IGeneratorBackend
{
    [GeneratedRegex("[\0-\b\v\f\u000e-\u001f]")]
    private partial Regex InvalidCharactersRegex();
    private IXLWorksheet Sheet { get; }

    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileExtension => "xlsx";

    private int currentRowIndex = 1;
    private int maxUsedColumn = 1;

    public ClosedXmlExcelBackend()
    {
        var workbook = new XLWorkbook();
        Sheet = workbook.Worksheets.Add("Sheet1");
    }

    public byte[] Generate()
    {
        Sheet.SheetView.FreezeRows(1);

        _ = Sheet.Columns(1, maxUsedColumn).AdjustToContents(minWidth: 5.0, maxWidth: 50.0);

        using var stream = new MemoryStream();
        Sheet.Workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public void WriteRow(IEnumerable<Cell> cells)
    {
        var columnIndex = 1;
        foreach (var cell in cells)
        {
            maxUsedColumn = Math.Max(maxUsedColumn, columnIndex);
            SetCell(columnIndex, cell);
            columnIndex++;
        }
        currentRowIndex++;
    }

    public void WriteHeaders(IEnumerable<ITableColumn> columns) => WriteRow(columns.Select(c => new Cell(c.Name)));

    internal const int ExcelMaxCellLength = 32767;

    internal static string TruncateIfTooLong(string value)
    {
        const string prefix = "(обрезано) ";
        const string suffix = "...";
        if (value.Length <= ExcelMaxCellLength)
        {
            return value;
        }
        return prefix + value[..(ExcelMaxCellLength - prefix.Length - suffix.Length)] + suffix;
    }

    private void SetCell(int columnIndex, Cell cell)
    {
        var xlCell = Sheet.Cell(currentRowIndex, columnIndex);

        switch (cell.Content)
        {
            case int num:
                _ = xlCell.SetValue(num);
                break;
            case DateTime time:
                _ = xlCell.SetValue(time);
                break;
            case Uri uri:
                _ = xlCell.SetValue(uri.AbsoluteUri);
                xlCell.GetHyperlink().ExternalAddress = uri;
                break;
            default:
                var strValue = InvalidCharactersRegex().Replace(cell.Content?.ToString() ?? "", "");
                _ = xlCell.SetValue(TruncateIfTooLong(strValue));
                break;
        }
    }
}
