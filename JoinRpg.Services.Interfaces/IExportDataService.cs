using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
  public interface IExportDataService
  {
    IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data);
    IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data, IGeneratorFrontend frontend);
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

  public enum CellType
  {
    Regular, Url, DateTime
  }

  public interface ITableColumn
  {
    string ExtractValue(object row);
    string Name { get; }
    CellType CellType { get; }
  }

  public interface IGeneratorFrontend
  {
    IEnumerable<ITableColumn> ParseColumns();
  }

}
