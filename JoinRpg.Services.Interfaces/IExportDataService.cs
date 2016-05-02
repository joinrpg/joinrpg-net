using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
  public interface IExportDataService
  {
    IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data);
    void BindDisplay<T>(Func<T, string> displayFunc);
  }

  public enum ExportType
  {
    Csv,
    ExcelXml
  }

  public interface IExportGenerator
  {
    Task<byte[]> Generate();
    string ContentType { get; }
    string FileExtension { get; }
  }

  public interface ITableColumn
  {
    string ExtractValue(object row);
    string Name { get; }
  }

  public interface IGeneratorFrontend
  {
    IEnumerable<ITableColumn> ParseColumns();
  }

}
