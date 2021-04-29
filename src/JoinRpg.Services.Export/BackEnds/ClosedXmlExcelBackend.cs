using System;
using System.IO;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using JoinRpg.Services.Export.Internal;

namespace JoinRpg.Services.Export.BackEnds
{
    internal class ClosedXmlExcelBackend : ExcelBackendBase
    {
        private readonly Regex _invalidCharactersRegex = new("[\x00-\x08\x0B\x0C\x0E-\x1F]");
        private IXLWorksheet Sheet { get; }

        public ClosedXmlExcelBackend()
        {
            var workbook = new XLWorkbook();
            Sheet = workbook.Worksheets.Add("Sheet1");
        }

        protected override void SetCell(int columnIndex, Cell cell)
        {
            var xlCell = Sheet.Cell(CurrentRowIndex, columnIndex);
            var uri = cell.Content as Uri;
            if (cell.Content is DateTime)
            {
                _ = xlCell.SetValue((DateTime)cell.Content);
            }
            else if (uri != null)
            {
                _ = xlCell.SetValue(uri.PathAndQuery);
                xlCell.Hyperlink.ExternalAddress = uri;
            }
            else
            {
                _ = xlCell.SetValue(_invalidCharactersRegex.Replace(cell.Content?.ToString() ?? "", ""));
            }
        }

        protected override void AdjustColumnToContent(int columnIndex) => Sheet.Column(columnIndex).AdjustToContents();

        protected override void FreezeHeader() => Sheet.SheetView.FreezeRows(1);

        protected override void SaveToStream(Stream stream) => Sheet.Workbook.SaveAs(stream);
    }
}
