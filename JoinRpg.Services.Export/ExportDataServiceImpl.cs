using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JoinRpg.Services.Export.Internal;
using JoinRpg.Services.Interfaces;

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
            return new ClosedXmlExcelBackend();
          default:
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
      }
    }
}
