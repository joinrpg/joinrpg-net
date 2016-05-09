using System;
using System.Collections.Generic;
using System.IO;
using JoinRpg.Services.Export.Internal;

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

    protected abstract void SaveToStream(Stream stream);

    public virtual void WriteRow(IEnumerable<Cell> cells)
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

    protected abstract void SetCell(int columnIndex, Cell cell);

    protected abstract void AdjustColumnToContent(int columnIndex);
  }
}
