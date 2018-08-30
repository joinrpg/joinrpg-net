using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using Microsoft.Practices.ObjectBuilder2;

namespace JoinRpg.Web.Models.Exporters
{
  public abstract class CustomExporter<TRow> : IGeneratorFrontend
  {
    protected CustomExporter(IUriService uriService)
    {
      UriService = uriService;
    }

    private class TableColumn<T> : ITableColumn
    {
      public TableColumn([CanBeNull] string name, [NotNull] Func<TRow, T> getter)
      {
        if (getter == null) throw new ArgumentNullException(nameof(getter));
        Name = name;
        Getter = getter;
      }

      public TableColumn([CanBeNull] PropertyInfo member, Func<TRow, T> getter)
        : this (member?.GetDisplayName(), getter)
      {
      }

      public object ExtractValue(object row)
      {
        return Getter((TRow) row);
      }

      public string Name { get; }

      [NotNull]
      private Func<TRow, T> Getter { get; }
    }

    private IUriService UriService { get; }

    public abstract IEnumerable<ITableColumn> ParseColumns();

    [MustUseReturnValue]
    protected ITableColumn StringColumn(Expression<Func<TRow, string>>  func)
    {
      return new TableColumn<string>(func.AsPropertyAccess(), func.Compile());
    }

    [Pure]
    protected ITableColumn UriColumn(Expression<Func<TRow, ILinkable>> func, string name = null)
    {
      return new TableColumn<Uri>(name ?? func.AsPropertyAccess()?.GetDisplayName(), row => UriService.GetUri(func.Compile()(row)));
    }

    [Pure]
    protected ITableColumn UriListColumn(Expression<Func<TRow, IEnumerable<ILinkable>>> func)
    {
      var compiledFunc = func.Compile();
      return new TableColumn<string>(func.AsPropertyAccess(),
        row => compiledFunc(row).Select(link => UriService.GetUri(link)).JoinStrings(" | "));
    }

    [Pure]
    protected ITableColumn StringListColumn(Expression<Func<TRow, IEnumerable<string>>> func, string name = null)
    {
      var compiledFunc = func.Compile();
      return new TableColumn<string>(name ?? func.AsPropertyAccess()?.GetDisplayName(),
        row => compiledFunc(row).JoinStrings(" | "));
    }

    [MustUseReturnValue]
    protected ITableColumn IntColumn([NotNull] Expression<Func<TRow, int>> func)
    {
      var member = func.AsPropertyAccess();
        var compiledFunc = func.Compile();
            return new TableColumn<int>(member, r => compiledFunc(r));
    }

    [MustUseReturnValue]
    protected ITableColumn IntColumn([NotNull] Expression<Func<TRow, int>> func, string name)
      {
          var compiledFunc = func.Compile();
            return new TableColumn<int>(name, r => compiledFunc(r));
      }

      [MustUseReturnValue]
      protected ITableColumn BoolColumn([NotNull] Expression<Func<TRow, bool>> func)
      {
          var memberName = func.AsPropertyAccess()?.GetDisplayName() ?? "1";
          return BoolColumn(func, memberName);
      }

      private static ITableColumn BoolColumn(Expression<Func<TRow, bool>> func, string name)
      {
          var compiledFunc = func.Compile();

          return new TableColumn<string>(func.AsPropertyAccess(), r => compiledFunc(r) ? name : "");
      }

      [MustUseReturnValue]
    protected ITableColumn EnumColumn(Expression<Func<TRow, Enum>> func)
    {
      var member = func.AsPropertyAccess();
      return new TableColumn<string>(member, r => func.Compile()(r).GetDisplayName());
    }

    [MustUseReturnValue]
    protected ITableColumn DateTimeColumn(Expression<Func<TRow, DateTime?>> func)
    {
      var member = func.AsPropertyAccess();
      return new TableColumn<DateTime?>(member, r => func.Compile()(r));
    }

    [MustUseReturnValue]
    protected IEnumerable<ITableColumn> UserColumn(Expression<Func<TRow, User>> func)
    {
        return ComplexColumn(
                func,
                u => u.GetDisplayName(),
                u => u.SurName,
                u => u.FatherName,
                u => u.BornName,
                u => u.Email)
            .Union(new[]
            {
                ComplexElementMemberColumn(func, u => u.Extra, e => e.Vk),
                ComplexElementMemberColumn(func, u => u.Extra, e => e.Skype),
                ComplexElementMemberColumn(func, u => u.Extra, e => e.Telegram),
                ComplexElementMemberColumn(func, u => u.Extra, e => e.Livejournal),
                ComplexElementMemberColumn(func, u => u.Extra, e => e.PhoneNumber),
            });
    }

    [MustUseReturnValue]
    protected ITableColumn ShortUserColumn(Expression<Func<TRow, User>> func, string name = null)
    {
      return ComplexElementMemberColumn(func, u => u.GetDisplayName(), name);
    }

    [MustUseReturnValue]
    private static IEnumerable<ITableColumn> ComplexColumn(Expression<Func<TRow, User>> func, params Expression<Func<User, string>>[] expressions)
    {
      return expressions.Select(expression => ComplexElementMemberColumn(func, expression));
    }

    [MustUseReturnValue]
    protected static ITableColumn ComplexElementMemberColumn<T, TOut>(Expression<Func<TRow, T>> complexGetter, Expression<Func<T, TOut>> expr, string name = null)
      where T: class
    {

      name = name ?? CombineName(complexGetter.AsPropertyAccess(), expr.AsPropertyAccess());
      return new TableColumn<TOut>(name, CombineGetters(complexGetter, expr).Compile());
    }

    [Pure]
    private static string CombineName(params PropertyInfo[] propertyAccessors)
    {
      return string.Join(".", propertyAccessors.Select(prop => prop?.GetDisplayName()));
    }

    [MustUseReturnValue]
    protected static ITableColumn ComplexElementMemberColumn<T1, T2, TOut>(Expression<Func<TRow, T1>> complexGetter,
      Expression<Func<T1, T2>> immed, Expression<Func<T2, TOut>> expr, string name = null) 
      where T2 : class
      where T1 : class
    {
      name = name ?? CombineName(complexGetter.AsPropertyAccess(), immed.AsPropertyAccess(), expr.AsPropertyAccess());
      return new TableColumn<TOut>(name, CombineGetters(CombineGetters(complexGetter, immed), expr).Compile());
    }

    private static Expression<Func<TRow, TOut>> CombineGetters<T, TOut>(Expression<Func<TRow, T>> complexGetter, Expression<Func<T, TOut>> expr)
      where T: class
    {
      //TODO: Combine getters before compile 
      return arg => complexGetter.Compile()(arg) == null ? default(TOut) : expr.Compile()(complexGetter.Compile()(arg));
    }

    [Pure]
    protected ITableColumn FieldColumn(ProjectField projectField, Func<TRow, IReadOnlyCollection<FieldWithValue>> fieldsFunc)
    {
      return FieldColumn(projectField, fieldsFunc, projectField.FieldName);
    }

    [Pure]
    protected static ITableColumn FieldColumn(ProjectField projectField, Func<TRow, IReadOnlyCollection<FieldWithValue>> fieldsFunc, string name)
    {
      return new TableColumn<string>(name,
        x => fieldsFunc(x).SingleOrDefault(f => f.Field.ProjectFieldId == projectField.ProjectFieldId)?.DisplayString);
    }
  }
}
