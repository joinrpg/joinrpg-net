using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using JoinRpg.Services.Export.Internal;
using JoinRpg.Services.Interfaces;
using Spire.Xls;

namespace JoinRpg.Services.Export
{
    [UsedImplicitly]
    public class ExportDataServiceImpl : IExportDataService
    {
      public IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data)
      {
        return new TableGenerator<T>(data, GetGeneratorBackend(type));
      }

      private static IGeneratorBackend GetGeneratorBackend(ExportType type)
      {
        switch (type)
        {
          case ExportType.Csv:
            return new CsvBackend();
          case ExportType.ExcelXml:
            return new SpireExcelBackend();
          default:
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
      }
    }

  internal class SpireExcelBackend : IGeneratorBackend
  {
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public string FileExtension => "xlsx";
    
    private IList<IEnumerable<Cell>> Rows { get; } = new List<IEnumerable<Cell>>();

    public void WriteRow(IEnumerable<Cell> cells)
    {
      Rows.Add(cells);
    }

    public byte[] Generate()
    {
      using (var book = new Spire.Xls.Workbook())
      {
        var sheet = book.Worksheets[0];
        for (var rowIndex = 0; rowIndex < Rows.Count; rowIndex ++)
        {
          var columnIndex = 0;
          foreach (var cell in Rows[rowIndex])
          {
            sheet.Range[rowIndex + 1, columnIndex + 1].Text = cell.Content;
            columnIndex++;
          }
        }
        using (var stream = new MemoryStream())
        { 
          
          book.SaveToStream(stream, FileFormat.Version2010);
          return stream.GetBuffer();
        }
      }
    }
  }
}
