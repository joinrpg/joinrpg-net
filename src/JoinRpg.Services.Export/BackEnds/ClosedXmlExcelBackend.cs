using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using JoinRpg.Services.Export.Internal;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.BackEnds
{
    internal class ClosedXmlExcelBackend : IGeneratorBackend
    {
        private readonly Regex _invalidCharactersRegex = new("[\x00-\x08\x0B\x0C\x0E-\x1F]");
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

            _ = Sheet.Columns(1, maxUsedColumn).AdjustToContents();

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

        private void SetCell(int columnIndex, Cell cell)
        {
            var xlCell = Sheet.Cell(currentRowIndex, columnIndex);

            switch (cell.Content)
            {
                case DateTime time:
                    _ = xlCell.SetValue(time);
                    break;
                case Uri uri:
                    _ = xlCell.SetValue(uri.PathAndQuery);
                    xlCell.Hyperlink.ExternalAddress = uri;
                    break;
                default:
                    _ = xlCell.SetValue(_invalidCharactersRegex.Replace(cell.Content?.ToString() ?? "", ""));
                    break;
            }
        }


    }
}
