using System.Data.Entity.Validation;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal class CharacterGroupService(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository
    ) : DbServiceImplBase(unitOfWork, currentUserAccessor), ICharacterGroupService
{
    public async Task<CharacterGroupIdentification> AddCharacterGroup(ProjectIdentification projectId,
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> parentCharacterGroupIds,
        string description)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);

        _ = project.RequestMasterAccess(CurrentUserId, Permission.CanEditRoles);
        _ = project.EnsureProjectActive();

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var group = Create(new CharacterGroup()
        {
            CharacterGroupName = Required(name),
            ParentCharacterGroupIds =
                ValidateCharacterGroupList(projectInfo,
                    Required(parentCharacterGroupIds)),
            ProjectId = projectId,
            IsRoot = false,
            IsSpecial = false,
            IsPublic = isPublic,
            IsActive = true,
            Description = new MarkdownDbValue(description),
        });

        MarkTreeModified(project);

        await UnitOfWork.SaveChangesAsync();

        return new CharacterGroupIdentification(projectId, group.CharacterGroupId);
    }

    public async Task EditCharacterGroup(CharacterGroupIdentification characterGroupId,
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> parentCharacterGroupIds,
        string description)
    {
        var characterGroup =
            (await ProjectRepository.GetGroupAsync(characterGroupId))
            .RequestMasterAccess(CurrentUserId, Permission.CanEditRoles)
            .EnsureProjectActive();

        if (characterGroup.IsRoot || characterGroup.IsSpecial)
        {
            throw new InvalidOperationException();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(characterGroupId.ProjectId);

        var currentGroupInfo = projectInfo.Groups[characterGroupId];
        var forbiddenParents = currentGroupInfo.AllChildGroupsIncludingThis;
        var cycleViolation = Required(parentCharacterGroupIds).FirstOrDefault(p => forbiddenParents.Contains(p));
        if (cycleViolation is not null)
        {
            throw new ArgumentException($"Группа {cycleViolation.CharacterGroupId} является потомком редактируемой группы и не может быть её родителем.");
        }

        characterGroup.CharacterGroupName = Required(name);
        characterGroup.IsPublic = isPublic;
        characterGroup.ParentCharacterGroupIds =
            ValidateCharacterGroupList(projectInfo,
                Required(parentCharacterGroupIds),
                ensureNotSpecial: true);
        characterGroup.Description = new MarkdownDbValue(description);

        MarkTreeModified(characterGroup.Project);
        MarkChanged(characterGroup);
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteCharacterGroup(CharacterGroupIdentification characterGroupId)
    {
        var characterGroup = await ProjectRepository.GetGroupAsync(characterGroupId) ?? throw new DbEntityValidationException();

        _ = characterGroup.RequestMasterAccess(CurrentUserId, Permission.CanEditRoles);
        _ = characterGroup.EnsureProjectActive();

        foreach (var character in characterGroup.Characters.Where(ch => ch.IsActive))
        {
            if (character.ParentCharacterGroupIds.Except([characterGroupId.CharacterGroupId]).Any())
            {
                continue;
            }

            character.ParentCharacterGroupIds = character.ParentCharacterGroupIds
                .Union(characterGroup.ParentCharacterGroupIds).ToArray();
        }

        foreach (var character in characterGroup.ChildGroups.Where(ch => ch.IsActive))
        {
            if (character.ParentCharacterGroupIds.Except([characterGroupId.CharacterGroupId]).Any())
            {
                continue;
            }

            character.ParentCharacterGroupIds = character.ParentCharacterGroupIds
                .Union(characterGroup.ParentCharacterGroupIds).ToArray();
        }

        MarkTreeModified(characterGroup.Project);
        MarkChanged(characterGroup);

        if (characterGroup.CanBePermanentlyDeleted)
        {
            characterGroup.DirectlyRelatedPlotElements.CleanLinksList();
        }

        _ = SmartDelete(characterGroup);

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task MoveCharacterGroup(CharacterGroupIdentification characterGroupId,
        CharacterGroupIdentification parentCharacterGroupId,
        short direction)
    {
        var parentCharacterGroup =
            await ProjectRepository.LoadGroupWithChildsAsync(parentCharacterGroupId.ProjectId.Value, parentCharacterGroupId.CharacterGroupId);
        _ = parentCharacterGroup.RequestMasterAccess(CurrentUserId, Permission.CanEditRoles);
        _ = parentCharacterGroup.EnsureProjectActive();

        var thisCharacterGroup =
            parentCharacterGroup.ChildGroups.Single(i =>
                i.CharacterGroupId == characterGroupId.CharacterGroupId);

        parentCharacterGroup.ChildGroupsOrdering = parentCharacterGroup
            .GetCharacterGroupsContainer().Move(thisCharacterGroup, direction).GetStoredOrder();
        await UnitOfWork.SaveChangesAsync();
    }
}
