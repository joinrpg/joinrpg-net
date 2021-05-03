using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters
{
    public static class ExportTypeNameParserHelper
    {
        public static ExportType? ToExportType(string? export)
        {
            return export switch
            {
                "csv" => ExportType.Csv,
                "xlsx" => ExportType.ExcelXml,
                _ => null,
            };
        }
    }
}
