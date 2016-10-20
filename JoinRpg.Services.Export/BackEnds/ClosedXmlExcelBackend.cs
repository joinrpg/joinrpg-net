using System;
using System.IO;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using JoinRpg.Services.Export.Internal;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.BackEnds
{
  internal class ClosedXmlExcelBackend : ExcelBackendBase
  {
    private readonly Regex _invalidCharactersRegex = new Regex("[\x00-\x08\x0B\x0C\x0E-\x1F]");
    private IXLWorksheet Sheet { get; }

    public ClosedXmlExcelBackend()
    {
      var workbook = new XLWorkbook();
      Sheet = workbook.Worksheets.Add("Sheet1");
    }

    protected override void SetCell(int columnIndex, Cell cell)
    {
      var xlCell = Sheet.Cell(CurrentRowIndex, columnIndex);
      xlCell.Value = _invalidCharactersRegex.Replace(cell.Content ?? "", "");
      switch (cell.CellType)
      {
        case CellType.Regular:
          break;
        case CellType.Url:
          xlCell.Hyperlink = new XLHyperlink(cell.Content);
          break;
        case CellType.DateTime:
          xlCell.DataType = XLCellValues.DateTime;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    protected override void AdjustColumnToContent(int columnIndex)
    {
      Sheet.Column(columnIndex).AdjustToContents();
    }

    protected override void FreezeHeader()
    {
      Sheet.SheetView.FreezeRows(1);
    }

    protected override void SaveToStream(Stream stream) => Sheet.Workbook.SaveAs(stream);
  }
}
