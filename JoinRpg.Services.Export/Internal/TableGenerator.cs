using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.Internal
{
  internal class TableGenerator<TRow> : IExportGenerator
  {
    private IEnumerable<TRow> Data { get; }
    private IGeneratorBackend Backend { get; }
    private IGeneratorFrontend Frontend { get; }

    public TableGenerator(IEnumerable<TRow> data, IGeneratorBackend backend, IGeneratorFrontend frontend)
    {
      Data = data;
      Backend = backend;
      Frontend = frontend;
    }

    public Task<byte[]> Generate()
    {
      //Run on background thread
      return Task.Run(() =>
      {
        
        var columns = Frontend.ParseColumns().ToList();

        Backend.WriteHeaders(columns);

        foreach (var row in Data)
        {
          Backend.WriteRow(columns.Select(tableColumn => new Cell()
          {
            Content = tableColumn.ExtractValue(row),
          }));
        }

        return Backend.Generate();
      });
    }

    public string ContentType => Backend.ContentType;

    public string FileExtension => Backend.FileExtension;
  }
}