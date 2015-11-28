using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Export.Internal
{
  internal class TableGenerator<TRow> : IExportGenerator
  {
    private IEnumerable<TRow> Data { get; set; }
    private IGeneratorBackend Backend { get; }

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
      var parameterExpression = Expression.Parameter(typeof (TRow));
      foreach (var propertyInfo in typeof (TRow).GetProperties())
      {
        var tableColumn = new TableColumn()
        {
          Name = propertyInfo.Name,
          Getter = Expression.Lambda<Func<TRow, object>>(
            Expression.Convert(
              Expression.Property(
                parameterExpression, propertyInfo), typeof (object)), parameterExpression).Compile()
        };

        var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
        if (displayAttribute != null)
        {
          tableColumn.Name = displayAttribute.Name ?? tableColumn.Name;
        }
        yield return tableColumn;
      }
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
          Content = Getter(row).ToString()
        };
      }

      public string Name { get; set; }

    public Func<TRow, object> Getter { get; set; }
    }

    public string ContentType => Backend.ContentType;

    public string FileExtension => Backend.FileExtension;
  }
}