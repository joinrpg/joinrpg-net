using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Helpers
{
  public abstract class CustomerExporter<TRow> : IGeneratorFrontend
  {
    private class TableColumn : ITableColumn
    {
      public TableColumn(string name, Func<TRow, string> getter)
      {
        Name = name;
        Getter = getter;
      }

      public TableColumn(PropertyInfo member, Func<TRow, string> getter)
        : this ((string) member.GetDisplayName(), getter)
      {
      }

      public string ExtractValue(object row)
      {
        return Getter((TRow) row);
      }

      [NotNull]
      public string Name { get; }

      [NotNull]
      private Func<TRow, string> Getter { get; }
    }

    public abstract IEnumerable<ITableColumn> ParseColumns();

    protected ITableColumn StringColumn(Expression<Func<TRow, string>>  func)
    {
      var member = func.AsPropertyAccess();
      return new TableColumn(member, func.Compile());
    }

    protected ITableColumn EnumColumn(Expression<Func<TRow, Enum>> func)
    {
      var member = func.AsPropertyAccess();
      return new TableColumn(member, r => func.Compile()(r).GetDisplayName());
    }

    protected ITableColumn DateTimeCOlumn(Expression<Func<TRow, DateTime?>> func)
    {
      var member = func.AsPropertyAccess();
      return new TableColumn(member, r => func.Compile()(r)?.ToString(CultureInfo.InvariantCulture));
    }

    protected ITableColumn UserColumn(Expression<Func<TRow, User>> func)
    {
      var member = func.AsPropertyAccess();
      return new TableColumn(member, r => func.Compile()(r)?.DisplayName);
    }

    protected ITableColumn FieldColumn(string name, Func<TRow, string> func)
    {
      return new TableColumn(name, func);
    }
  }
}