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

namespace JoinRpg.Web.Helpers
{
  public abstract class CustomExporter<TRow> : IGeneratorFrontend
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

    [MustUseReturnValue]
    protected ITableColumn StringColumn(Expression<Func<TRow, string>>  func)
    {
      var member = func.AsPropertyAccess();
      return new TableColumn(member, func.Compile());
    }

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
      return new TableColumn(member, r => func.Compile()(r)?.ToString(CultureInfo.InvariantCulture));
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
    protected IEnumerable<ITableColumn> ShortUserColumn(Expression<Func<TRow, User>> func)
    {
      return ComplexColumn(func, u => u.DisplayName);
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
    private static ITableColumn ComplexElementMemberColumn<T>(Expression<Func<TRow, T>> complexGetter, Expression<Func<T, string>> expr)
    {
      return
        new TableColumn(
          $"{complexGetter.AsPropertyAccess().GetDisplayName()}.{expr.AsPropertyAccess().GetDisplayName()}",
          CombineGetters(complexGetter, expr).Compile());
    }

    [MustUseReturnValue]
    private static ITableColumn ComplexElementMemberColumn<T1, T2>(Expression<Func<TRow, T1>> complexGetter,
      Expression<Func<T1, T2>> immed, Expression<Func<T2, string>> expr) where T2 : class
    {
      return
        new TableColumn(
          $"{complexGetter.AsPropertyAccess().GetDisplayName()}.{immed.AsPropertyAccess().GetDisplayName()}.{expr.AsPropertyAccess().GetDisplayName()}",
          CombineGetters(CombineGetters(complexGetter, immed), expr).Compile());
    }

    private static Expression<Func<TRow, TOut>> CombineGetters<T, TOut>(Expression<Func<TRow, T>> complexGetter, Expression<Func<T, TOut>> expr)
      where TOut : class 
    {
      //TODO: Combine getters before compile 
      return arg => complexGetter.Compile()(arg) == null ? null : expr.Compile()(complexGetter.Compile()(arg));
    }
  }
}