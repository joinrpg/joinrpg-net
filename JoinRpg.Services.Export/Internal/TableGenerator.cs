using System;
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

    private IDictionary<Type, Func<object, string>> DisplayFunctions { get; } =
      new Dictionary<Type, Func<object, string>>();

    private ISet<Type> ComplexTypes { get; } = new HashSet<Type>();

    public TableGenerator(IEnumerable<TRow> data, IGeneratorBackend backend)
    {
      Data = data;
      Backend = backend;
    }

    public Task<byte[]> Generate()
    {
      //Run on background thread
      return Task.Run(() =>
      {
        var columnCreator = new ColumnCreator(DisplayFunctions, ComplexTypes, typeof(TRow), o => o);
        var columns = columnCreator.ParseColumns().ToList();

        var headerCells = columns.Select(c => c.CreateHeader());
        Backend.WriteRow(headerCells);

        foreach (var row in Data)
        {
          Backend.WriteRow(columns.Select(tableColumn => tableColumn.ExtractValue(row)));
        }

        return Backend.Generate();
      });
    }

    public string ContentType => Backend.ContentType;

    public string FileExtension => Backend.FileExtension;
    public IExportGenerator BindDisplay<T>(Func<T, string> displayFunc)
    {
      DisplayFunctions.Add(typeof(T), arg =>  displayFunc((T) arg));
      return this;
    }

    public IExportGenerator RegisterComplexType<T>()
    {
      ComplexTypes.Add(typeof(T));
      return this;
    }
  }

  internal class TableColumn
  {
    public Cell CreateHeader()
    {
      return new Cell()
      {
        Content = Name,
        ColumnHeader = true
      };
    }

    public Cell ExtractValue(object row)
    {
      return new Cell()
      {
        Content = Converter(Getter(row))?.ToString(),
        IsUri = IsUri
      };
    }

    public string Name { get; set; }

    public Func<object, object> Getter { private get; set; }

    public Func<object, object> Converter
    { private get; set; } = arg => arg;
    public bool IsUri { get; set; }
  }
}