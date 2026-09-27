namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Проекция вводных в <see cref="PlotElementRow"/>.
/// </summary>
internal static class PlotElementRowQuery
{
    /// <summary>
    /// Отбирает поля вводной вместе с одной версией текста.
    /// </summary>
    /// <param name="elements">Вводные, которые нужно выбрать.</param>
    /// <param name="version">
    /// Версия для показа; <c>null</c> — последняя. Страницы папки всегда показывают последнюю,
    /// конкретную версию запрашивает только страница истории.
    /// </param>
    /// <remarks>
    /// Номер отображаемой версии считается в отдельном <c>Select</c>, а не подставляется в каждое
    /// подвыражение: иначе <c>e.Texts.Max(...)</c> пришлось бы повторять пять раз подряд.
    /// </remarks>
    public static IQueryable<PlotElementRow> ToRows(this IQueryable<PlotElement> elements, int? version = null)
        => elements
            .Select(e => new { Element = e, Shown = version ?? e.Texts.Max(t => t.Version) })
            .Select(x => new PlotElementRow
            {
                ProjectId = x.Element.ProjectId,
                PlotFolderId = x.Element.PlotFolderId,
                PlotElementId = x.Element.PlotElementId,
                PlotFolderMasterTitle = x.Element.PlotFolder.MasterTitle,

                ElementType = x.Element.ElementType,
                IsMasterOnly = x.Element.IsMasterOnly,
                IsActive = x.Element.IsActive,
                IsCompleted = x.Element.IsCompleted,
                PublishedVersion = x.Element.Published,

                LastVersionNumber = x.Element.Texts.Max(t => t.Version),
                LastVersionTodoField = x.Element.Texts
                    .Where(t => t.Version == x.Element.Texts.Max(m => m.Version))
                    .Select(t => t.TodoField)
                    .FirstOrDefault(),

                CurrentVersionNumber = x.Shown,
                CurrentContent = x.Element.Texts
                    .Where(t => t.Version == x.Shown).Select(t => t.Content.Contents).FirstOrDefault(),
                CurrentTodoField = x.Element.Texts
                    .Where(t => t.Version == x.Shown).Select(t => t.TodoField).FirstOrDefault(),
                CurrentModifiedAt = x.Element.Texts
                    .Where(t => t.Version == x.Shown).Select(t => t.ModifiedDateTime).FirstOrDefault(),

                PrevVersionModifiedAt = x.Element.Texts
                    .Where(t => t.Version == x.Shown - 1)
                    .Select(t => (DateTime?)t.ModifiedDateTime).FirstOrDefault(),
                NextVersionModifiedAt = x.Element.Texts
                    .Where(t => t.Version == x.Shown + 1)
                    .Select(t => (DateTime?)t.ModifiedDateTime).FirstOrDefault(),

                Author = x.Element.Texts
                    .Where(t => t.Version == x.Shown && t.AuthorUser != null)
                    .Select(t => new PlotAuthorRow
                    {
                        UserId = t.AuthorUser.UserId,
                        Preffered = t.AuthorUser.PrefferedName,
                        Born = t.AuthorUser.BornName,
                        Sur = t.AuthorUser.SurName,
                        Father = t.AuthorUser.FatherName,
                        EmailAddress = t.AuthorUser.Email,
                    })
                    .FirstOrDefault(),

                Characters = x.Element.TargetCharacters
                    .Select(c => new PlotTargetRow { Id = c.CharacterId, Name = c.CharacterName }),
                Groups = x.Element.TargetGroups
                    .Select(g => new PlotTargetRow { Id = g.CharacterGroupId, Name = g.CharacterGroupName }),
            });
}
