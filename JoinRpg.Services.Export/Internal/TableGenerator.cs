using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.Internal
{
  internal class TableGenerator<TRow> : IExportGenerator
  {
    private readonly ParameterExpression _rowParameterExpression;
    private IEnumerable<TRow> Data { get; }
    private IGeneratorBackend Backend { get; }

    private IDictionary<Type, Func<object, object>> DisplayFunctions { get; } =
      new Dictionary<Type, Func<object, object>>();

    public TableGenerator(IEnumerable<TRow> data, IGeneratorBackend backend)
    {
      Data = data;
      Backend = backend;
      _rowParameterExpression = Expression.Parameter(typeof (TRow));
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
      foreach (var propertyInfo in typeof (TRow).GetProperties())
      {
        var displayColumnAttribute = propertyInfo.DeclaringType.GetCustomAttribute<DisplayColumnAttribute>();

        if (displayColumnAttribute != null)
        {
          yield return GetTableColumn(propertyInfo.DeclaringType?.GetProperty(displayColumnAttribute.DisplayColumn) ?? propertyInfo);
        }


        yield return GetTableColumn(propertyInfo);
      }
    }

    private TableColumn GetTableColumn([NotNull] PropertyInfo propertyInfo)
    {
      var tableColumn = new TableColumn()
      {
        Name = propertyInfo.Name,
        Getter = Expression.Lambda<Func<TRow, object>>(
          Expression.Convert(
            Expression.Property(
              _rowParameterExpression, propertyInfo), typeof (object)), _rowParameterExpression).Compile()
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

      foreach (var displayFunction in DisplayFunctions)
      {
        if (displayFunction.Key.IsAssignableFrom(propertyInfo.PropertyType))
        {
          tableColumn.Converter = displayFunction.Value;
          break;
        }
      }
      return tableColumn;
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
          Content = Converter(Getter(row)).ToString(),
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
    public IExportGenerator BindDisplay<T>(Func<T, object> displayFunc)
    {
      DisplayFunctions.Add(typeof(T), arg =>  displayFunc((T) arg));
      return this;
    }
  }
}