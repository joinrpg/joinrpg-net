using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JoinRpg.Services.Export.AutoFrontEnd;
using JoinRpg.Services.Export.BackEnds;
using JoinRpg.Services.Export.Internal;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export
{
    [UsedImplicitly]
    public class ExportDataServiceImpl : IExportDataService
    {
        private Dictionary<Type, Func<object, string>> DisplayFunctions { get; } = new ();

        [Obsolete]
        public IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data) where T : class
            => GetGenerator(type, data, new AutoFrontend<T>(DisplayFunctions));

        public IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data, IGeneratorFrontend frontend)
             where T : class
            => new TableGenerator<T>(data, GetGeneratorBackend(type), frontend);

        [Obsolete]
        public void BindDisplay<T>(Func<T, string> displayFunc) => DisplayFunctions.Add(typeof(T), arg => displayFunc((T)arg));

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
