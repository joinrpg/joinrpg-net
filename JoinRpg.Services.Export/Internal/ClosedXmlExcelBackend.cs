using System.IO;
using ClosedXML.Excel;

namespace JoinRpg.Services.Export.Internal
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
      Sheet.Cell(CurrentRowIndex, columnIndex).Value = cell.Content;
    }

    protected override void AdjustColumnToContent(int columnIndex)
    {
      Sheet.Column(columnIndex).AdjustToContents();
    }

    protected override void SaveToStream(Stream stream) => Sheet.Workbook.SaveAs(stream);
  }
}
