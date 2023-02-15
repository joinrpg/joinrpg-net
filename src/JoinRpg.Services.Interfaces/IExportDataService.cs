namespace JoinRpg.Services.Interfaces;

public interface IExportDataService
{
    IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data, IGeneratorFrontend<T> frontend) where T : class;
}

public enum ExportType
{
    Csv,
    ExcelXml,
}

public interface IExportGenerator
{
    byte[] Generate();
    string ContentType { get; }
    string FileExtension { get; }
}

public interface ITableColumn
{
    object? ExtractValue(object row);
    string? Name { get; }
}

public interface IGeneratorFrontend<TRow>
{
    IEnumerable<ITableColumn> ParseColumns();
}

