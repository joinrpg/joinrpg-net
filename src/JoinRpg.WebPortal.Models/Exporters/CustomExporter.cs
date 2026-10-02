using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.UserProfile;

namespace JoinRpg.Web.Models.Exporters;

public abstract class CustomExporter<TRow>(IUriService uriService) : IGeneratorFrontend<TRow>
{
    private class TableColumn<T> : ITableColumn
    {
        public TableColumn(string? name, Func<TRow, T?> getter)
        {
            Name = name;
            Getter = getter ?? throw new ArgumentNullException(nameof(getter));
        }

        public TableColumn(PropertyInfo? member, Func<TRow, T?> getter)
          : this(member?.GetDisplayName(), getter)
        {
        }

        public object? ExtractValue(object row) => Getter((TRow)row);

        public string? Name { get; }

        private Func<TRow, T?> Getter { get; }
    }

    public abstract IEnumerable<ITableColumn> ParseColumns();

    [Pure]
    protected ITableColumn StringColumn(Expression<Func<TRow, string?>> func) => new TableColumn<string>(func.AsPropertyAccess(), func.Compile());

    [Pure]
    protected ITableColumn UriColumn(Expression<Func<TRow, ILinkable>> func, string? name = null) => new TableColumn<Uri>(name ?? func.AsPropertyAccess()?.GetDisplayName(), row => uriService.GetUri(func.Compile()(row)));

    [Pure]
    protected ITableColumn UriListColumn(Expression<Func<TRow, IEnumerable<ILinkable>>> func)
    {
        var compiledFunc = func.Compile();
        return new TableColumn<string>(func.AsPropertyAccess(),
          row => compiledFunc(row).Select(link => uriService.GetUri(link).ToString()).JoinStrings(" | "));
    }

    [Pure]
    protected ITableColumn StringListColumn(Expression<Func<TRow, IEnumerable<string>>> func, string? name = null)
    {
        var compiledFunc = func.Compile();
        return new TableColumn<string>(name ?? func.AsPropertyAccess()?.GetDisplayName(),
          row => compiledFunc(row).JoinStrings(" | "));
    }

    [Pure]
    protected ITableColumn IntColumn(Expression<Func<TRow, int>> func)
    {
        var member = func.AsPropertyAccess();
        var compiledFunc = func.Compile();
        return new TableColumn<int>(member, r => compiledFunc(r));
    }

    [Pure]
    protected ITableColumn IntColumn(Expression<Func<TRow, int>> func, string name)
    {
        var compiledFunc = func.Compile();
        return new TableColumn<int>(name, r => compiledFunc(r));
    }

    [Pure]
    protected ITableColumn BoolColumn(Expression<Func<TRow, bool>> func)
    {
        var memberName = func.AsPropertyAccess()?.GetDisplayName() ?? "1";
        return BoolColumn(func, memberName);
    }

    private static TableColumn<string> BoolColumn(Expression<Func<TRow, bool>> func, string name)
    {
        var compiledFunc = func.Compile();

        return new TableColumn<string>(func.AsPropertyAccess(), r => compiledFunc(r) ? name : "");
    }

    [Pure]
    protected ITableColumn EnumColumn<TEnum>(Expression<Func<TRow, Nullable<TEnum>>> func) where TEnum : struct, Enum
    {
        var member = func.AsPropertyAccess();
        return new TableColumn<string>(member, r => func.Compile()(r)?.GetDisplayName());
    }

    [Pure]
    protected ITableColumn EnumColumn<TEnum>(Expression<Func<TRow, TEnum>> func) where TEnum : struct, Enum
    {
        var member = func.AsPropertyAccess();
        return new TableColumn<string>(member, r => func.Compile()(r).GetDisplayName());
    }

    [Pure]
    protected ITableColumn DateTimeColumn(Expression<Func<TRow, DateTime?>> func)
    {
        var member = func.AsPropertyAccess();
        return new TableColumn<DateTime?>(member, r => func.Compile()(r));
    }

    [Pure]
    protected IEnumerable<ITableColumn> UserColumn(Expression<Func<TRow, User?>> func, ProjectInfo projectInfo)
    {
        if (projectInfo.ProjectStatus == ProjectLifecycleStatus.Archived)
        {
            return [ShortUserColumn(func)];
        }
        return ComplexColumn(
                func,
                u => u.GetDisplayName(),
                u => u.SurName,
                u => u.FatherName,
                u => u.BornName,
                u => u.Email)
            .Union(
            [
            VkColumn(func),
            TelegramColumn(func),
            ComplexElementMemberColumn(func, u => u.Extra, e => e.Livejournal),
            ComplexElementMemberColumn(func, u => u.Extra, e => e.PhoneNumber),
            ]);
    }

    /// <summary>
    /// Те же колонки, что и версия для EF-сущности, но поверх доменного <see cref="UserInfo"/>.
    /// </summary>
    /// <remarks>
    /// Колонки собираются вручную, а не через <c>ComplexElementMemberColumn</c>: части имени
    /// и контакты в <see cref="UserInfo"/> — опциональные value-типы, а дерево выражений не
    /// допускает в себе <c>?.</c>. Заголовки (включая легаси-сегмент <c>.Extra.</c>) оставлены
    /// ровно теми же, что были у версии для сущности: мастера выгружают эти таблицы годами и
    /// разбирают их по именам колонок.
    /// </remarks>
    [Pure]
    protected IEnumerable<ITableColumn> UserColumn(Expression<Func<TRow, UserInfo?>> func, ProjectInfo projectInfo)
    {
        if (projectInfo.ProjectStatus == ProjectLifecycleStatus.Archived)
        {
            return [ShortUserColumn(func)];
        }

        var prefix = CombineName(func.AsPropertyAccess());
        var getter = func.Compile();

        return
        [
            new TableColumn<string>(prefix, row => getter(row)?.DisplayName.DisplayName),
            new TableColumn<string>($"{prefix}.SurName", row => getter(row)?.UserFullName.SurName?.Value),
            new TableColumn<string>($"{prefix}.FatherName", row => getter(row)?.UserFullName.FatherName?.Value),
            new TableColumn<string>($"{prefix}.BornName", row => getter(row)?.UserFullName.BornName?.Value),
            new TableColumn<string>($"{prefix}.Email", row => getter(row)?.Email.Value),
            new TableColumn<Uri>("ВК", row => getter(row)?.Social.Vk?.Link),
            new TableColumn<Uri>("Телеграм", row => getter(row)?.Social.Telegram?.Link),
            new TableColumn<string>($"{prefix}.Extra.Livejournal", row => getter(row)?.Social.LiveJournal?.Value),
            new TableColumn<string>($"{prefix}.Extra.PhoneNumber", row => getter(row)?.PhoneNumber),
        ];
    }

    [Pure]
    protected ITableColumn ShortUserColumn(Expression<Func<TRow, User?>> func, string? name = null) => ComplexElementMemberColumn(func, u => u.GetDisplayName(), name);

    /// <summary>
    /// Та же колонка, что и версия для EF-сущности, но поверх <see cref="ProjectMasterInfo"/>.
    /// </summary>
    /// <remarks>
    /// Собирается руками, а не через <c>ComplexElementMemberColumn</c>, именно из-за заголовка.
    /// У версии для сущности вложенное выражение — вызов метода (<c>u => u.GetDisplayName()</c>),
    /// который <c>AsPropertyAccess</c> не распознаёт, поэтому заголовок остаётся одним префиксом
    /// («Ответственный»). Здесь отображаемое имя достаётся обращением к свойству, и тот же путь
    /// дописал бы к заголовку «.DisplayName» — а заголовки выгрузок менять нельзя, мастера
    /// разбирают таблицы по именам колонок.
    /// </remarks>
    [Pure]
    protected ITableColumn ShortUserColumn(Expression<Func<TRow, ProjectMasterInfo?>> func, string? name = null)
    {
        name ??= CombineName(func.AsPropertyAccess());
        var getter = func.Compile();
        return new TableColumn<string>(name, row => getter(row)?.Name.DisplayName);
    }

    [Pure]
    protected ITableColumn ShortUserColumn(Expression<Func<TRow, UserLinkViewModel?>> func, string? name = null) => ComplexElementMemberColumn(func, u => u.DisplayName, name);

    /// <summary>
    /// Та же колонка, что и версия для EF-сущности, но поверх <see cref="UserInfo"/>.
    /// </summary>
    /// <remarks>
    /// Собирается руками по той же причине, что и версия для <see cref="ProjectMasterInfo"/>:
    /// через <c>ComplexElementMemberColumn</c> заголовок по умолчанию получал бы хвост
    /// «.DisplayName», которого не было у версии для сущности — там имя достаётся вызовом метода,
    /// а <c>AsPropertyAccess</c> вызовы не распознаёт. Видно это на архивном проекте: от игрока
    /// там остаётся ровно эта колонка.
    /// </remarks>
    [Pure]
    protected ITableColumn ShortUserColumn(Expression<Func<TRow, UserInfo?>> func, string? name = null)
    {
        name ??= CombineName(func.AsPropertyAccess());
        var getter = func.Compile();
        return new TableColumn<string>(name, row => getter(row)?.DisplayName.DisplayName);
    }

    [Pure]
    protected ITableColumn VkColumn(Expression<Func<TRow, User?>> func) => new TableColumn<Uri>("ВК", user => UserSocialLink.GetVKUri(func.Compile()(user)?.Extra?.Vk)?.Uri);

    [Pure]
    protected ITableColumn TelegramColumn(Expression<Func<TRow, User?>> func) => new TableColumn<Uri>("Телеграм", user => UserSocialLink.GetTelegramUri(func.Compile()(user)?.Extra?.Telegram)?.Uri);

    [Pure]
    private static IEnumerable<ITableColumn> ComplexColumn(Expression<Func<TRow, User?>> func, params Expression<Func<User, string?>>[] expressions) => expressions.Select(expression => ComplexElementMemberColumn(func, expression));

    [Pure]
    protected static ITableColumn ComplexElementMemberColumn<T, TOut>(Expression<Func<TRow, T?>> complexGetter, Expression<Func<T, TOut>> expr, string? name = null)
      where T : class
    {

        name ??= CombineName(complexGetter.AsPropertyAccess(), expr.AsPropertyAccess());
        return new TableColumn<TOut>(name, CombineGetters(complexGetter, expr).Compile());
    }

    [Pure]
    private static string CombineName(params PropertyInfo?[] propertyAccessors) => propertyAccessors.Select(prop => prop?.GetDisplayName()).JoinIfNotNullOrWhitespace(".");

    [Pure]
    protected static ITableColumn ComplexElementMemberColumn<T1, T2, TOut>(Expression<Func<TRow, T1?>> complexGetter,
      Expression<Func<T1, T2?>> immed, Expression<Func<T2, TOut>> expr, string? name = null)
      where T2 : class
      where T1 : class
    {
        name ??= CombineName(complexGetter.AsPropertyAccess(), immed.AsPropertyAccess(), expr.AsPropertyAccess());
        return new TableColumn<TOut>(name, CombineGetters(CombineGetters(complexGetter, immed), expr).Compile());
    }

    private static Expression<Func<TRow, TOut?>> CombineGetters<T, TOut>(Expression<Func<TRow, T?>> complexGetter, Expression<Func<T, TOut>> expr)
      where T : class
    {
        //TODO: Combine getters before compile 
        return arg => complexGetter.Compile()(arg) == null ? default(TOut) : expr.Compile()(complexGetter.Compile()(arg)!);
    }

    [Pure]
    protected ITableColumn FieldColumn(ProjectFieldInfo projectField, Func<TRow, IReadOnlyCollection<FieldWithValue>> fieldsFunc) => FieldColumn(projectField, fieldsFunc, projectField.Name);

    [Pure]
    protected ITableColumn FieldColumn(ProjectFieldInfo projectField, Func<TRow, IReadOnlyDictionary<ProjectFieldIdentification, string>> fieldsFunc)
    {
        return new TableColumn<string>(projectField.Name, x => fieldsFunc(x)[projectField.Id]);
    }

    [Pure]
    protected static ITableColumn FieldColumn(ProjectFieldInfo projectField, Func<TRow, IReadOnlyCollection<FieldWithValue>> fieldsFunc, string name)
    {
        return new TableColumn<string>(name,
          x => fieldsFunc(x).SingleOrDefault(f => f.Field == projectField)?.DisplayString);
    }
}
