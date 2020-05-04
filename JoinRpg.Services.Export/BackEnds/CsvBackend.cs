using System.Collections.Generic;
using System.Linq;
using System.Text;
using JoinRpg.Services.Export.Internal;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.BackEnds
{
    internal class CsvBackend : IGeneratorBackend
    {
        private StringBuilder Builder { get; } = new StringBuilder();
        public string ContentType => "text/csv";

        public string FileExtension => "csv";

        public void WriteHeaders(IEnumerable<ITableColumn> columns) => Builder.AppendLine(string.Join(",", columns.Select(c => EscapeForCsv(c.Name))));

        public void WriteRow(IEnumerable<Cell> cells)
          => Builder.AppendLine(string.Join(",", cells.Select(GetContentForCsv)));

        private static string GetContentForCsv(Cell c) => EscapeForCsv(c.Content?.ToString());

        private static string EscapeForCsv(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }
            //According to specificaton just quoting should be enough to work with line breaks
            //Sadly, Excel thinks otherwise, so let's remove line breaks.
            //Line breaks should be present in HTML fields only, and <br> will be enough for them
            return "\"" +
              value
                .Replace("\"", "\"\"")
                .Replace("\n", " ")
              + "\"";
        }

        public byte[] Generate() => Encoding.UTF8.GetBytes(Builder.ToString());
    }
}
