using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JoinRpg.Services.Export.Internal;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.BackEnds
{
  internal abstract class ExcelBackendBase : IGeneratorBackend
  {
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileExtension => "xlsx";
    protected int CurrentRowIndex { get; private set;  } = 1;
    private int MaxUsedColumn { get; set; } = 1;

    public byte[] Generate()
    {
      FreezeHeader();
      for (var columnIndex = 1; columnIndex <= MaxUsedColumn; columnIndex++)
      {
        AdjustColumnToContent(columnIndex);
      }
      using (var stream = new MemoryStream())
      {
        SaveToStream(stream);
        return stream.ToArray();
      }
    }

    public void WriteRow(IEnumerable<Cell> cells)
    {
      var columnIndex = 1;
      foreach (var cell in cells)
      {
        MaxUsedColumn = Math.Max(MaxUsedColumn, columnIndex);
        SetCell(columnIndex, cell);
        columnIndex++;
      }
      CurrentRowIndex++;
    }
    protected abstract void SaveToStream(Stream stream);

    protected abstract void SetCell(int columnIndex, Cell cell);

    protected abstract void AdjustColumnToContent(int columnIndex);

    protected abstract void FreezeHeader();

    public void WriteHeaders(IEnumerable<ITableColumn> columns)
    {
      var headerCells = columns.Select(c => new Cell()
      {
        Content = c.Name,
        ColumnHeader = true,
      });
      WriteRow(headerCells);
    }
  }
}
