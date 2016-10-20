using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters
{
  public abstract class CustomExporter<TRow> : IGeneratorFrontend
  {
    private class TableColumn : ITableColumn
    {
      public TableColumn(string name, Func<TRow, string> getter, CellType type = CellType.Regular)
      {
        Name = name;
        Getter = getter;
        CellType = type;
      }

      public TableColumn(PropertyInfo member, Func<TRow, string> getter, CellType type = CellType.Regular)
        : this (member.GetDisplayName(), getter, type)
      {
      }

      public string ExtractValue(object row)
      {
        return Getter((TRow) row);
      }

      [NotNull]
      public string Name { get; }

      public CellType CellType { get; }

      [NotNull]
      private Func<TRow, string> Getter { get; }
    }

    public abstract IEnumerable<ITableColumn> ParseColumns();

    [MustUseReturnValue]
    protected ITableColumn StringColumn(Expression<Func<TRow, string>>  func)
    {
      var member = func.AsPropertyAccess();
      return new TableColumn(member, func.Compile());
    }

    [MustUseReturnValue]
    protected ITableColumn IntColumn([NotNull] Expression<Func<TRow, int>> func)
    {
      var member = func.AsPropertyAccess();
      return new TableColumn(member, r => func.Compile()(r).ToString());
    }

    [MustUseReturnValue]
    protected ITableColumn IntColumn([NotNull] Expression<Func<TRow, int>> func, string name)
      => new TableColumn(name, r => func.Compile()(r).ToString());

    [MustUseReturnValue]
    protected ITableColumn EnumColumn(Expression<Func<TRow, Enum>> func)
    {
      var member = func.AsPropertyAccess();
      return new TableColumn(member, r => func.Compile()(r).GetDisplayName());
    }

    [MustUseReturnValue]
    protected ITableColumn DateTimeColumn(Expression<Func<TRow, DateTime?>> func)
    {
      var member = func.AsPropertyAccess();
      return new TableColumn(member, r => func.Compile()(r)?.ToString(CultureInfo.InvariantCulture), CellType.DateTime);
    }

    [MustUseReturnValue]
    protected IEnumerable<ITableColumn> UserColumn(Expression<Func<TRow, User>> func)
    {
      return ComplexColumn(
          func,
          u => u.DisplayName,
          u => u.SurName,
          u => u.FatherName,
          u => u.BornName,
          u => u.Email)
        .Union(new[]
          {ComplexElementMemberColumn(func, u => u.Extra, e => e.Vk)});
    }

    [MustUseReturnValue]
    protected ITableColumn ShortUserColumn(Expression<Func<TRow, User>> func, string name = null)
    {
      return ComplexElementMemberColumn(func, u => u.DisplayName, name);
    }

    [MustUseReturnValue]
    protected ITableColumn FieldColumn(string name, Func<TRow, string> func)
    {
      return new TableColumn(name, func);
    }

    [MustUseReturnValue]
    private static IEnumerable<ITableColumn> ComplexColumn(Expression<Func<TRow, User>> func, params Expression<Func<User, string>>[] expressions)
    {
      return expressions.Select(expression => ComplexElementMemberColumn(func, expression));
    }

    [MustUseReturnValue]
    protected static ITableColumn ComplexElementMemberColumn<T>(Expression<Func<TRow, T>> complexGetter, Expression<Func<T, string>> expr, string name = null)
    {
      name = name ?? $"{complexGetter.AsPropertyAccess().GetDisplayName()}.{expr.AsPropertyAccess().GetDisplayName()}";
      return new TableColumn(name, CombineGetters(complexGetter, expr).Compile());
    }

    [MustUseReturnValue]
    protected static ITableColumn ComplexElementMemberColumn<T1, T2>(Expression<Func<TRow, T1>> complexGetter,
      Expression<Func<T1, T2>> immed, Expression<Func<T2, string>> expr, string name = null) where T2 : class
    {
      name = name ??
             $"{complexGetter.AsPropertyAccess().GetDisplayName()}.{immed.AsPropertyAccess().GetDisplayName()}.{expr.AsPropertyAccess().GetDisplayName()}";
      return new TableColumn(name, CombineGetters(CombineGetters(complexGetter, immed), expr).Compile());
    }

    private static Expression<Func<TRow, TOut>> CombineGetters<T, TOut>(Expression<Func<TRow, T>> complexGetter, Expression<Func<T, TOut>> expr)
      where TOut : class 
    {
      //TODO: Combine getters before compile 
      return arg => complexGetter.Compile()(arg) == null ? null : expr.Compile()(complexGetter.Compile()(arg));
    }
  }
}