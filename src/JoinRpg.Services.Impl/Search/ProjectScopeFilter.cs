using System.Linq.Expressions;
using LinqKit;

namespace JoinRpg.Services.Impl.Search;

/// <summary>
/// Сужение поискового запроса до одного проекта.
/// </summary>
internal static class ProjectScopeFilter
{
    /// <summary>
    /// Если <paramref name="projectId"/> задан — добавляет фильтр по проекту, иначе отдаёт запрос как есть.
    /// <para>
    /// Почему отдельный <c>Where</c>, а не <c>projectId == null || e.ProjectId == projectId.Value</c>
    /// прямо в предикате: EF6 при разборе дерева выражений вычисляет замыкания без короткого замыкания
    /// оператора <c>||</c>, поэтому при глобальном поиске (<c>projectId == null</c>) он всё равно лезет
    /// за <c>projectId.Value</c> у null-ссылки и падает с
    /// <c>TargetException: Non-static method requires a target</c>. EF Core такое переживает, EF6 — нет.
    /// Поэтому в дерево выражений попадает уже развёрнутый <see cref="int"/>, а не
    /// <see cref="ProjectIdentification"/>?.
    /// </para>
    /// </summary>
    public static IQueryable<T> FilterByProject<T>(
        this IQueryable<T> query,
        ProjectIdentification? projectId,
        Expression<Func<T, int>> projectIdSelector)
    {
        if (projectId is null)
        {
            return query;
        }

        var projectIdValue = projectId.Value;
        return query.AsExpandable().Where(entity => projectIdSelector.Invoke(entity) == projectIdValue);
    }
}
