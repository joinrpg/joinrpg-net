using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl.Projects;

/// <summary>
/// База контекста доменной операции над проектом: согласованное время операции и текущий
/// пользователь. Даёт общие аудит-хелперы и созданию, и изменению проекта.
/// </summary>
internal abstract record ProjectOperationContext(DateTimeOffset Now, ICurrentUserAccessor CurrentUser);

/// <summary>
/// Контекст изменения метаданных существующего проекта: добавляет трекаемую сущность, снимок
/// метаданных и окончательное удаление под-сущностей. Негенерик — чтобы приватные хелперы сервисов
/// принимали его без параметра типа.
/// </summary>
/// <param name="Project">Трекаемая EF-сущность проекта; её и нужно мутировать.</param>
/// <param name="ProjectInfo">Снимок метаданных ДО изменения.</param>
/// <param name="Now">Время выполнения операции</param>
/// <param name="CurrentUser">Текущий пользователь</param>
/// <param name="RemovePermanently">Окончательное удаление под-сущности из того же DbContext (см. <see cref="ProjectOperationContextExtensions.SmartDelete"/>).</param>
internal abstract record ProjectMutationContext(
    Project Project,
    ProjectInfo ProjectInfo,
    DateTimeOffset Now,
    ICurrentUserAccessor CurrentUser,
    Action<object> RemovePermanently) : ProjectOperationContext(Now, CurrentUser);

/// <summary>
/// Контекст изменения метаданных проекта с типизированными аргументами операции (<see cref="Request"/>).
/// </summary>
internal sealed record ProjectMutationContext<TArgs>(
    Project Project,
    ProjectInfo ProjectInfo,
    DateTimeOffset Now,
    ICurrentUserAccessor CurrentUser,
    TArgs Request,
    Action<object> RemovePermanently)
    : ProjectMutationContext(Project, ProjectInfo, Now, CurrentUser, RemovePermanently);

/// <summary>
/// Контекст создания нового проекта: существующего <see cref="Project"/>/<see cref="ProjectInfo"/>
/// ещё нет, фабрика строит сущность с нуля.
/// </summary>
internal sealed record ProjectCreationContext<TArgs>(
    DateTimeOffset Now,
    ICurrentUserAccessor CurrentUser,
    TArgs Request) : ProjectOperationContext(Now, CurrentUser);

internal static class ProjectOperationContextExtensions
{
    public static void MarkCreatedNow(this ProjectOperationContext ctx, ICreatedUpdatedTrackedForEntity entity)
        => EntityAudit.MarkCreated(entity, ctx.Now.UtcDateTime, ctx.CurrentUser.UserId);

    public static void MarkChanged(this ProjectOperationContext ctx, ICreatedUpdatedTrackedForEntity entity)
        => EntityAudit.MarkChanged(entity, ctx.Now.UtcDateTime, ctx.CurrentUser.UserId);

    /// <summary>
    /// Резолвит группу для изменения: возвращает трекаемую сущность и её метаданные, проверяет, что
    /// группа обычная (<see cref="CharacterGroupType.Regular"/>) — root и специальные группы менять
    /// нельзя — и помечает сущность изменённой.
    /// </summary>
    public static (CharacterGroup Group, CharacterGroupInfo GroupInfo) GetCharacterGroupForChange(
        this ProjectMutationContext ctx, CharacterGroupIdentification id)
    {
        var group = ctx.Project.CharacterGroups.Single(g => g.CharacterGroupId == id.CharacterGroupId);
        var groupInfo = ctx.ProjectInfo.Groups[id];
        if (groupInfo.GroupType != CharacterGroupType.Regular)
        {
            throw new InvalidOperationException();
        }
        ctx.MarkChanged(group);
        return (group, groupInfo);
    }

    /// <summary>
    /// Резолвит сетку ролей для изменения: возвращает трекаемую EF-сущность и её доменный снимок.
    /// В отличие от <see cref="GetCharacterGroupForChange"/> не помечает сущность изменённой —
    /// <see cref="DataModel.ProjectRolesList"/> не несёт аудит-полей.
    /// </summary>
    public static (DataModel.ProjectRolesList Entity, DomainTypes.ProjectMetadata.ProjectRolesList Info) GetProjectRolesListForChange(
        this ProjectMutationContext ctx, ProjectRolesListIdentification id)
    {
        var entity = ctx.Project.ProjectRolesLists
            .SingleOrDefault(x => x.ProjectRolesListId == id.ProjectRolesListId)
            ?? throw new JoinRpgEntityNotFoundException(id.ProjectRolesListId, "ProjectRolesList");
        var info = ctx.ProjectInfo.GetRolesListById(id);
        return (entity, info);
    }

    /// <summary>
    /// «Умное» удаление под-сущности существующего проекта: permanent — через DbContext контекста,
    /// иначе soft-delete (<c>IsActive = false</c>).
    /// </summary>
    public static bool SmartDelete<T>(this ProjectMutationContext ctx, T? field)
        where T : class, IDeletableSubEntity
        => EntityDeletion.SmartDelete<T>(field, ctx.RemovePermanently);
}
