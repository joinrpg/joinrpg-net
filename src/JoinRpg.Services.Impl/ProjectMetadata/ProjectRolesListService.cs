using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces.ProjectMetadata;

namespace JoinRpg.Services.Impl.ProjectMetadata;

internal class ProjectRolesListService(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    ILogger<ProjectRolesListService> logger)
    : DbServiceImplBase(unitOfWork, currentUserAccessor), IProjectRolesListService
{

    public async Task<ProjectRolesList> CreateAsync(ProjectRolesList model)
    {
        logger.LogInformation("Создаём сетку ролей {Name} для проекта {ProjectId}", model.Name, model.ProjectRolesListId.ProjectId);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(model.ProjectRolesListId.ProjectId);
        projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageClaims);
        ValidateShowCharacterGroups(model, projectInfo);

        // Для создания игнорируем переданный ID, база данных сгенерирует новый
        var entity = CreateEntity(model);
        UpdateEntity(entity, model);

        UnitOfWork.GetDbSet<DataModel.ProjectRolesList>().Add(entity);
        await UnitOfWork.SaveChangesAsync();

        logger.LogInformation("Создана сетка ролей {ProjectRolesListId} с именем {Name}", entity.ProjectRolesListId, model.Name);

        return ToDomain(entity);
    }

    public async Task<ProjectRolesList> UpdateAsync(ProjectRolesList model)
    {
        logger.LogInformation("Обновляем сетку ролей {ProjectRolesListId} (проект {ProjectId})", model.ProjectRolesListId.ProjectRolesListId, model.ProjectRolesListId.ProjectId);

        var (entity, projectInfo) = await GetEntityOrThrow(model.ProjectRolesListId);
        ValidateShowCharacterGroups(model, projectInfo);
        UpdateEntity(entity, model);

        await UnitOfWork.SaveChangesAsync();

        logger.LogInformation("Обновлена сетка ролей {ProjectRolesListId} с именем {Name}", entity.ProjectRolesListId, model.Name);

        return ToDomain(entity);
    }

    public async Task RemoveAsync(ProjectRolesListIdentification id)
    {
        var (entity, _) = await GetEntityOrThrow(id);

        logger.LogInformation("Удаляем сетку ролей {ProjectRolesListId} (проект {ProjectId}) с именем {Name}",
            entity.ProjectRolesListId, entity.ProjectId, entity.Name);

        UnitOfWork.GetDbSet<DataModel.ProjectRolesList>().Remove(entity);
        await UnitOfWork.SaveChangesAsync();

        logger.LogInformation("Удалена сетка ролей {ProjectRolesListId}", entity.ProjectRolesListId);
    }

    public async Task<ProjectRolesList> GetByIdAsync(ProjectRolesListIdentification id)
    {
        var (entity, _) = await GetEntityOrThrow(id);
        return ToDomain(entity);
    }

    private async Task<(DataModel.ProjectRolesList entity, ProjectInfo projectInfo)> GetEntityOrThrow(ProjectRolesListIdentification id)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(id.ProjectId);
        projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageClaims);

        var entity = await UnitOfWork.GetDbSet<DataModel.ProjectRolesList>()
            .Where(prl => prl.ProjectId == id.ProjectId.Value && prl.ProjectRolesListId == id.ProjectRolesListId)
            .SingleOrDefaultAsync();

        if (entity == null)
        {
            logger.LogWarning("Сетка ролей {ProjectRolesListId} не найдена в проекте {ProjectId}", id.ProjectRolesListId, id.ProjectId);
            throw new JoinRpgEntityNotFoundException(id.ProjectRolesListId, "ProjectRolesList");
        }

        return (entity, projectInfo);
    }

    private static DataModel.ProjectRolesList CreateEntity(ProjectRolesList model)
    {
        return new DataModel.ProjectRolesList
        {
            ProjectId = model.ProjectRolesListId.ProjectId.Value,
            ProjectRolesListId = model.ProjectRolesListId.ProjectRolesListId,
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
        entity.ShowCharacterGroups = model.ShowCharacterGroups;
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
            ShowCharacterGroups: entity.ShowCharacterGroups
        );
    }

    private static void ValidateShowCharacterGroups(ProjectRolesList model, ProjectInfo projectInfo)
    {
        if (!model.ShowCharacterGroups || model.CharacterGroupId is not { } groupId)
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
