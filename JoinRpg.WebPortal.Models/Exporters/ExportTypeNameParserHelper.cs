using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters
{
    public static class ExportTypeNameParserHelper
    {
        public static ExportType? ToExportType(string export)
        {
            switch (export)
            {
                case "csv": return ExportType.Csv;
                case "xlsx": return ExportType.ExcelXml;
                default: return null;
            }
        }
    }
}
