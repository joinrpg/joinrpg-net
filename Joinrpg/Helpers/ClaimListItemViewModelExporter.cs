using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

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
        : this (member.GetDisplayName(), getter)
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

  public class ClaimListItemViewModelExporter: CustomerExporter<ClaimListItemViewModel>
  {
    public ClaimListItemViewModelExporter(ICollection<ProjectField> fields)
    {
      Fields = fields;
    }

    private ICollection<ProjectField> Fields { get; }

    public  override IEnumerable<ITableColumn> ParseColumns()
    {
      yield return StringColumn(x => x.Name);
      yield return EnumColumn(x => x.ClaimStatus);
      yield return DateTimeCOlumn(x => x.UpdateDate);
      yield return UserColumn(x => x.LastModifiedBy);
      yield return UserColumn(x => x.Responsible);
      yield return UserColumn(x => x.Player);

      foreach (var projectField in Fields)
      {
        yield return
          FieldColumn(projectField.FieldName,
            x => x.Fields.Fields.SingleOrDefault(f => f.ProjectFieldId == projectField.ProjectFieldId)?.DisplayString);
      }
    }

  }

  
}