using JoinRpg.Data.Interfaces.ProjectMetadata;

namespace JoinRpg.Dal.Impl.Repositories.ProjectMetadata;

internal class ProjectRolesListRepository(MyDbContext ctx) : IProjectRolesListRepository
{
    public async Task<IReadOnlyCollection<DomainTypes.ProjectMetadata.ProjectRolesList>> GetForProjectAsync(ProjectIdentification projectId)
    {
        var entities = await ctx.Set<DataModel.ProjectRolesList>()
            .Where(prl => prl.ProjectId == projectId.Value)
            .ToListAsync();

        return entities.Select(ToDomain).ToList();
    }

    public async Task<DomainTypes.ProjectMetadata.ProjectRolesList?> GetByIdAsync(ProjectRolesListIdentification id)
    {
        var entity = await ctx.Set<DataModel.ProjectRolesList>()
            .Where(prl => prl.ProjectId == id.ProjectId.Value && prl.ProjectRolesListId == id.ProjectRolesListId)
            .FirstOrDefaultAsync();

        return entity == null ? null : ToDomain(entity);
    }

    private static DomainTypes.ProjectMetadata.ProjectRolesList ToDomain(DataModel.ProjectRolesList entity)
    {
        var projectId = new ProjectIdentification(entity.ProjectId);
        var fields = entity.FieldIds
            .Select(fieldId => new ProjectFieldIdentification(projectId, fieldId))
            .ToList();

        return new DomainTypes.ProjectMetadata.ProjectRolesList(
            ProjectRolesListId: new ProjectRolesListIdentification(projectId, entity.ProjectRolesListId),
            Name: entity.Name,
            CharacterGroupId: CharacterGroupIdentification.FromOptional(projectId, entity.CharacterGroupId),
            PublicMode: entity.PublicMode,
            Fields: fields,
            ContactsColumn: entity.ContactsColumn,
            GroupsColumn: entity.GroupsColumn,
            ShowCharacterGroups: entity.ShowCharacterGroups
        );
    }
}
