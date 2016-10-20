using System;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.AutoFrontEnd
{
  internal class TableColumn : ITableColumn
  {
    public string ExtractValue(object row)
    {
      return Converter(Getter(row))?.ToString();
    }

    public string Name { get; set; }
    public CellType CellType { get; set; }

    public Func<object, object> Getter { private get; set; }

    public Func<object, object> Converter
    { private get; set; } = arg => arg;
  }
}