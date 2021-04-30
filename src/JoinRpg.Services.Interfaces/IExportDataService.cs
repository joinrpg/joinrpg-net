using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace JoinRpg.Services.Interfaces
{
    public interface IExportDataService
    {
        [Obsolete]
        IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data) where T : class;
        IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data, IGeneratorFrontend<T> frontend) where T : class;
        void BindDisplay<T>(Func<T, string> displayFunc);
    }

    public enum ExportType
    {
        Csv,
        ExcelXml,
    }

    public interface IExportGenerator
    {
        Task<byte[]> Generate();
        string ContentType { get; }
        string FileExtension { get; }
    }

    public interface ITableColumn
    {
        [CanBeNull]
        object? ExtractValue(object row);
        [CanBeNull]
        string? Name { get; }
    }

    public interface IGeneratorFrontend<TRow>
    {
        IEnumerable<ITableColumn> ParseColumns();
    }

}
