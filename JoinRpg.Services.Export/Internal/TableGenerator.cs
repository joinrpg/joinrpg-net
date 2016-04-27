using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Helpers;
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
      Expression.Parameter(typeof (TRow));
    }

    public Task<byte[]> Generate()
    {
      //Run on background thread
      return Task.Run(() =>
      {
        var columns = ParseColumns().ToList();

        var headerCells = columns.Select(c => c.CreateHeader());
        Backend.WriteRow(headerCells);

        foreach (var row in Data)
        {
          Backend.WriteRow(columns.Select(tableColumn => tableColumn.ExtractValue(row)));
        }

        return Backend.Generate();
      });
    }

    private IEnumerable<TableColumn> ParseColumns()
    {
      var type = typeof (TRow);
      foreach (var propertyInfo in type.GetProperties())
      {
        var displayColumnAttribute = propertyInfo.DeclaringType.GetCustomAttribute<DisplayColumnAttribute>();

        if (displayColumnAttribute != null)
        {
          yield return
            GetTableColumn(propertyInfo.DeclaringType?.GetProperty(displayColumnAttribute.DisplayColumn) ?? propertyInfo);
        }
        else
        {
          yield return GetTableColumn(propertyInfo);
        }
      }
    }

    private TableColumn GetTableColumn([NotNull] PropertyInfo propertyInfo)
    {
      var tableColumn = new TableColumn
      {
        Name = propertyInfo.Name,
        Getter = LambdaHelpers.CompileGetter<TRow>(propertyInfo)
      };

      var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
      if (displayAttribute != null)
      {
        tableColumn.Name = displayAttribute.Name ?? tableColumn.Name;
      }

      if (propertyInfo.GetCustomAttribute<UrlAttribute>() != null)
      {
        tableColumn.IsUri = true;
      }

      tableColumn.Converter = GetConverterForType(propertyInfo.PropertyType);

      return tableColumn;
    }

    private Func<object, string> GetConverterForType(Type propertyType)
    {
      if (propertyType.IsEnum)
      {
        return o => ((Enum) o).GetDisplayName();
      }
      if (typeof(string).IsAssignableFrom(propertyType))
      {
        return o => (string)o; //Prevent futher conversion
      }

      var enumerableArg = LambdaHelpers.GetEnumerableType(propertyType);
      if (enumerableArg != null)
      {
        return LambdaHelpers.GetEnumerableConvertor(GetConverterForType(enumerableArg));
      }

      if (typeof(IEnumerable).IsAssignableFrom(propertyType))
      {
        return LambdaHelpers.GetEnumerableConvertor(item => item.ToString());
      }
      return
        DisplayFunctions.Where(
          displayFunction => displayFunction.Key.IsAssignableFrom(propertyType))
          .Select(kv => kv.Value)
          .FirstOrDefault() ?? (arg => arg?.ToString());
    }

    private class TableColumn
    {
      public Cell CreateHeader()
      {
        return new Cell()
        {
          Content = Name,
          ColumnHeader = true
        };
      }

      public Cell ExtractValue(TRow row)
      {
        return new Cell()
        {
          Content = Converter(Getter(row))?.ToString(),
          IsUri = IsUri
        };
      }

      public string Name { get; set; }

      public Func<TRow, object> Getter { private get; set; }

      public Func<object, object> Converter
      { private get; set; } = arg => arg;
      public bool IsUri { get; set; }
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
}