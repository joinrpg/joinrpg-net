using System.IO;
using ClosedXML.Excel;
using JoinRpg.Services.Export.Internal;

namespace JoinRpg.Services.Export.BackEnds
{
  internal class ClosedXmlExcelBackend : ExcelBackendBase
  {
    private IXLWorksheet Sheet { get; }

    public ClosedXmlExcelBackend()
    {
      var workbook = new XLWorkbook();
      Sheet = workbook.Worksheets.Add("Sheet1");
    }

    protected override void SetCell(int columnIndex, Cell cell)
    {
      var xlCell = Sheet.Cell(CurrentRowIndex, columnIndex);
      xlCell.Value = cell.Content;
      if (cell.IsUri)
      {
        xlCell.Hyperlink = new XLHyperlink(cell.Content);
      }
    }

    protected override void AdjustColumnToContent(int columnIndex)
    {
      Sheet.Column(columnIndex).AdjustToContents();
    }

    protected override void SaveToStream(Stream stream) => Sheet.Workbook.SaveAs(stream);
  }
}
