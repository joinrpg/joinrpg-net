using System;
using System.Collections.Generic;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.AutoFrontEnd
{
  internal class AutoFrontend<TRow> : IGeneratorFrontend
  {
    public AutoFrontend(IDictionary<Type, Func<object, string>> displayFunctions)
    {
      DisplayFunctions = displayFunctions;
    }

    public IEnumerable<ITableColumn> ParseColumns()
    {
      return new ColumnCreator(DisplayFunctions, typeof(TRow)).ParseColumns();
    }

    private IDictionary<Type, Func<object, string>> DisplayFunctions { get; }
  }
}