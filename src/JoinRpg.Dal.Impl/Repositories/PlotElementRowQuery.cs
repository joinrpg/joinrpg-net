using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Проекция вводных в <see cref="PlotElementRow"/>.
/// </summary>
internal static class PlotElementRowQuery
{
    /// <summary>
    /// Отбирает поля вводной вместе с одной версией текста.
    /// </summary>
    /// <param name="version">
    /// Версия для показа; <c>null</c> — последняя. Страницы папки всегда показывают последнюю,
    /// конкретную версию запрашивает только страница истории.
    /// </param>
    /// <remarks>
    /// Выражение, а не готовый <c>IQueryable</c>: так его можно подставить и во вложенную проекцию
    /// (вводные внутри папки) через LinqKit — <c>Invoke</c> на стороне <c>AsExpandable</c>-запроса.
    /// Вызов обычного extension-метода EF6 бы там не стерпел.
    ///
    /// Номер отображаемой версии приходится повторять в каждом подвыражении: промежуточный
    /// <c>Select</c>, который вычислил бы его один раз, в одно выражение не укладывается.
    /// </remarks>
    public static Expression<Func<PlotElement, PlotElementRow>> RowSelector(int? version)
        => e => new PlotElementRow
        {
            ProjectId = e.ProjectId,
            PlotFolderId = e.PlotFolderId,
            PlotElementId = e.PlotElementId,
            PlotFolderMasterTitle = e.PlotFolder.MasterTitle,

            ElementType = e.ElementType,
            IsMasterOnly = e.IsMasterOnly,
            IsActive = e.IsActive,
            IsCompleted = e.IsCompleted,
            PublishedVersion = e.Published,

            LastVersionNumber = e.Texts.Max(t => t.Version),
            LastVersionTodoField = e.Texts
                .Where(t => t.Version == e.Texts.Max(m => m.Version))
                .Select(t => t.TodoField).FirstOrDefault(),

            CurrentVersionNumber = version ?? e.Texts.Max(t => t.Version),
            CurrentVersionExists = e.Texts.Any(t => t.Version == (version ?? e.Texts.Max(m => m.Version))),
            CurrentContent = e.Texts
                .Where(t => t.Version == (version ?? e.Texts.Max(m => m.Version)))
                .Select(t => t.Content.Contents).FirstOrDefault(),
            CurrentTodoField = e.Texts
                .Where(t => t.Version == (version ?? e.Texts.Max(m => m.Version)))
                .Select(t => t.TodoField).FirstOrDefault(),
            CurrentModifiedAt = e.Texts
                .Where(t => t.Version == (version ?? e.Texts.Max(m => m.Version)))
                .Select(t => t.ModifiedDateTime).FirstOrDefault(),

            PrevVersionModifiedAt = e.Texts
                .Where(t => t.Version == (version ?? e.Texts.Max(m => m.Version)) - 1)
                .Select(t => (DateTime?)t.ModifiedDateTime).FirstOrDefault(),
            NextVersionModifiedAt = e.Texts
                .Where(t => t.Version == (version ?? e.Texts.Max(m => m.Version)) + 1)
                .Select(t => (DateTime?)t.ModifiedDateTime).FirstOrDefault(),

            // Автор проецируется сущностью: имя из неё собирает UserTransformationExtensions,
            // заводить для этого ещё один тип-строку незачем.
            Author = e.Texts
                .Where(t => t.Version == (version ?? e.Texts.Max(m => m.Version)))
                .Select(t => t.AuthorUser).FirstOrDefault(),

            Characters = e.TargetCharacters
                .Select(c => new PlotTargetRow { Id = c.CharacterId, Name = c.CharacterName }),
            Groups = e.TargetGroups
                .Select(g => new PlotTargetRow { Id = g.CharacterGroupId, Name = g.CharacterGroupName }),
        };

    /// <summary>Проецирует вводные, показывая указанную версию (<c>null</c> — последнюю).</summary>
    public static IQueryable<PlotElementRow> ToRows(this IQueryable<PlotElement> elements, int? version = null)
        => elements.AsExpandable().Select(RowSelector(version));
}
