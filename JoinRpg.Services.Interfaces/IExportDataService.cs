using System.Collections.Generic;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
  public interface IExportDataService
  {
    IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data);
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
}
