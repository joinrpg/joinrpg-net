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

        var entity = await GetEntityOrThrow(model.ProjectRolesListId);
        UpdateEntity(entity, model);

        await UnitOfWork.SaveChangesAsync();

        logger.LogInformation("Обновлена сетка ролей {ProjectRolesListId} с именем {Name}", entity.ProjectRolesListId, model.Name);

        return ToDomain(entity);
    }

    public async Task RemoveAsync(ProjectRolesListIdentification id)
    {
        var entity = await GetEntityOrThrow(id);

        logger.LogInformation("Удаляем сетку ролей {ProjectRolesListId} (проект {ProjectId}) с именем {Name}",
            entity.ProjectRolesListId, entity.ProjectId, entity.Name);

        UnitOfWork.GetDbSet<DataModel.ProjectRolesList>().Remove(entity);
        await UnitOfWork.SaveChangesAsync();

        logger.LogInformation("Удалена сетка ролей {ProjectRolesListId}", entity.ProjectRolesListId);
    }

    private async Task<DataModel.ProjectRolesList> GetEntityOrThrow(ProjectRolesListIdentification id)
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

        return entity;
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
            GroupsColumn: entity.GroupsColumn
        );
    }
}
