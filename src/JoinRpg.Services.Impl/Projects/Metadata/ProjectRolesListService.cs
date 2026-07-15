using JoinRpg.Domain;
using JoinRpg.Services.Interfaces.ProjectMetadata;

namespace JoinRpg.Services.Impl.Projects.Metadata;

internal class ProjectRolesListService(
    IProjectPropsService projectPropsService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<ProjectRolesListService> logger)
    : IProjectRolesListService
{
    public async Task<ProjectRolesList> CreateAsync(ProjectRolesList model)
    {
        logger.LogInformation("Создаём сетку ролей {Name} для проекта {ProjectId}", model.Name, model.ProjectRolesListId.ProjectId);

        var entity = await projectPropsService.ChangeProjectProperties(
            model.ProjectRolesListId.ProjectId,
            Permission.CanManageClaims,
            ProjectActiveRequirement.MustBeActive,
            model,
            ctx =>
            {
                ValidateGroupsViewMode(ctx.Request, ctx.ProjectInfo);

                // Для создания игнорируем переданный ID, база данных сгенерирует новый
                var entity = CreateEntity(ctx.Request);
                UpdateEntity(entity, ctx.Request);
                ctx.Project.ProjectRolesLists.Add(entity);
                return entity;
            });

        logger.LogInformation("Создана сетка ролей {ProjectRolesListId} с именем {Name}", entity.ProjectRolesListId, model.Name);

        // ProjectRolesListId генерируется БД при SaveChanges — читаем уже после возврата из сервиса.
        return ToDomain(entity);
    }

    public async Task<ProjectRolesList> UpdateAsync(ProjectRolesList model)
    {
        logger.LogInformation("Обновляем сетку ролей {ProjectRolesListId} (проект {ProjectId})", model.ProjectRolesListId.ProjectRolesListId, model.ProjectRolesListId.ProjectId);

        var entity = await projectPropsService.ChangeProjectProperties(
            model.ProjectRolesListId.ProjectId,
            Permission.CanManageClaims,
            ProjectActiveRequirement.MustBeActive,
            model,
            ctx =>
            {
                var (entity, _) = ctx.GetProjectRolesListForChange(ctx.Request.ProjectRolesListId);
                ValidateGroupsViewMode(ctx.Request, ctx.ProjectInfo);
                UpdateEntity(entity, ctx.Request);
                return entity;
            });

        logger.LogInformation("Обновлена сетка ролей {ProjectRolesListId} с именем {Name}", entity.ProjectRolesListId, model.Name);

        return ToDomain(entity);
    }

    public async Task RemoveAsync(ProjectRolesListIdentification id)
    {
        await projectPropsService.ChangeProjectProperties(
            id.ProjectId,
            Permission.CanManageClaims,
            ProjectActiveRequirement.MustBeActive,
            id,
            ctx =>
            {
                var (entity, _) = ctx.GetProjectRolesListForChange(ctx.Request);

                logger.LogInformation("Удаляем сетку ролей {ProjectRolesListId} (проект {ProjectId}) с именем {Name}",
                    entity.ProjectRolesListId, entity.ProjectId, entity.Name);

                ctx.RemovePermanently(entity);
            });

        logger.LogInformation("Удалена сетка ролей {ProjectRolesListId}", id.ProjectRolesListId);
    }

    public async Task<ProjectRolesList> GetByIdAsync(ProjectRolesListIdentification id)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(id.ProjectId);
        projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageClaims);
        return projectInfo.GetRolesListById(id);
    }

    private static DataModel.ProjectRolesList CreateEntity(ProjectRolesList model)
    {
        return new DataModel.ProjectRolesList
        {
            ProjectId = model.ProjectRolesListId.ProjectId.Value,
        };
    }

    private static void UpdateEntity(DataModel.ProjectRolesList entity, ProjectRolesList model)
    {
        entity.Name = model.Name;
        entity.CharacterGroupId = model.CharacterGroupId?.CharacterGroupId;
        entity.PublicMode = model.PublicMode;
        entity.FieldIds = model.Fields.Select(f => f.ProjectFieldId).ToArray();
        entity.ContactsColumn = model.ContactsColumn;
        entity.GroupsColumn = model.GroupsColumn;
        entity.GroupsViewMode = model.GroupsViewMode;
        entity.ShowRolesFilter = model.ShowRolesFilter;
    }

    private static ProjectRolesList ToDomain(DataModel.ProjectRolesList entity)
    {
        var projectId = new ProjectIdentification(entity.ProjectId);
        var fields = entity.FieldIds
            .Select(fieldId => new ProjectFieldIdentification(projectId, fieldId))
            .ToList();

        return new ProjectRolesList(
            ProjectRolesListId: new ProjectRolesListIdentification(projectId, entity.ProjectRolesListId),
            Name: entity.Name,
            CharacterGroupId: entity.CharacterGroupId.HasValue
                ? new CharacterGroupIdentification(projectId, entity.CharacterGroupId.Value)
                : null,
            PublicMode: entity.PublicMode,
            Fields: fields,
            ContactsColumn: entity.ContactsColumn,
            GroupsColumn: entity.GroupsColumn,
            GroupsViewMode: entity.GroupsViewMode,
            ShowRolesFilter: entity.ShowRolesFilter
        );
    }

    private static void ValidateGroupsViewMode(ProjectRolesList model, ProjectInfo projectInfo)
    {
        if (model.GroupsViewMode == RolesGridGroupsViewMode.None || model.CharacterGroupId is not { } groupId)
        {
            return;
        }

        var group = projectInfo.GetGroupById(groupId.CharacterGroupId);
        if (group.GroupType == CharacterGroupType.SpecialToValue)
        {
            throw new InvalidOperationException(
                "Нельзя показывать группы для спец-группы-значения. Выберите обычную или корневую группу.");
        }
    }
}
