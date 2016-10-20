using System.Collections.Generic;
using System.Linq;
using System.Text;
using JoinRpg.Services.Export.Internal;

namespace JoinRpg.Services.Export.BackEnds
{
  internal class CsvBackend : IGeneratorBackend
  {
    private StringBuilder Builder { get;  }= new StringBuilder();
    public string ContentType => "text/csv";

    public string FileExtension => "csv";

    public void WriteRow(IEnumerable<Cell> cells)
      => Builder.AppendLine(string.Join(",", cells.Select(GetContentForCsv)));

    private static string GetContentForCsv(Cell c)
    {
      var value = c.Content?.ToString();
      if (string.IsNullOrWhiteSpace(value))
      {
        return "";
      }
      return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    public byte[] Generate() => Encoding.UTF8.GetBytes(Builder.ToString());
  }
}