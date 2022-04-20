using JetBrains.Annotations;
using JoinRpg.Services.Export.BackEnds;
using JoinRpg.Services.Export.Internal;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export
{
    [UsedImplicitly]
    public class ExportDataServiceImpl : IExportDataService
    {
        public IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data, IGeneratorFrontend<T> frontend)
             where T : class
            => new TableGenerator<T>(data, GetGeneratorBackend(type), frontend);

        private static IGeneratorBackend GetGeneratorBackend(ExportType type)
        {
            return type switch
            {
                ExportType.Csv => new CsvBackend(),
                ExportType.ExcelXml => new ClosedXmlExcelBackend(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }
    }
}
