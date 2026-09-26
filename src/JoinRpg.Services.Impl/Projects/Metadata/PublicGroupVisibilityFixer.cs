using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects.Metadata;

/// <summary>
/// Разовая починка данных под issue #4878: публичным группам, у которых нет публичного пути
/// наверх, снимается публичность.
/// </summary>
/// <remarks>
/// Почему скрываем, а не открываем родителей: непубличная группа скрыта осознанно, а её публичный
/// потомок снаружи всё равно не виден — то есть фактическое поведение уже «скрыто», и мы приводим
/// данные к нему, ничего не раскрывая наружу.
/// </remarks>
[Obsolete("Разовая починка данных под #4878, удалить вместе с ней — см. #4974")]
internal class PublicGroupVisibilityFixer(IProjectPropsService projectPropsService) : IPublicGroupVisibilityFixer
{
    /// <summary>
    /// Скрывает нарушителей в одном проекте и возвращает их названия.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Нарушения считаются один раз: скрытие нарушителя не может сделать нарушителем кого-то ещё.
    /// Публичный потомок нарушителя либо уже нарушитель сам (путь через него и так непубличен),
    /// либо у него есть другой публичный путь, которого мы не трогаем.
    /// </para>
    /// <para>
    /// <see cref="ProjectActiveRequirement.AllowInactive"/>: 215 из 244 нарушений на проде живут
    /// в архивных проектах. Их метаданные никто не редактирует, но публичный JSON ролей у них
    /// отдаётся, поэтому чинить надо и их.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyCollection<string>> HideGroupsWithoutPublicPath(ProjectIdentification projectId)
        => await projectPropsService.ChangeProjectProperties(
            projectId,
            Permission.CanEditRoles,
            ProjectActiveRequirement.AllowInactive,
            projectId,
            ctx =>
            {
                var violations = PublicGroupPathRule.FindViolations(ctx.ProjectInfo.GroupTree);

                foreach (var violation in violations)
                {
                    var (group, _) = ctx.GetCharacterGroupForChange(violation.Id);
                    group.IsPublic = false;
                }

                return (IReadOnlyCollection<string>)[.. violations.Select(group => group.Name)];
            });
}
