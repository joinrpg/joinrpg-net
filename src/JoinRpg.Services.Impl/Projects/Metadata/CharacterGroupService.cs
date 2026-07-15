using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects.Metadata;

internal class CharacterGroupService(IProjectPropsService projectPropsService) : ICharacterGroupService
{
    public async Task<CharacterGroupIdentification> AddCharacterGroup(ProjectIdentification projectId,
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> parentCharacterGroupIds,
        string description)
    {
        var group = await projectPropsService.ChangeProjectProperties(
            projectId,
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            (name, isPublic, parentCharacterGroupIds, description),
            ctx =>
            {
                var group = new CharacterGroup()
                {
                    CharacterGroupName = ServiceValidation.Required(ctx.Request.name),
                    ParentCharacterGroupIds =
                        ctx.ProjectInfo.ValidateCharacterGroupList(
                            ServiceValidation.Required(ctx.Request.parentCharacterGroupIds)),
                    ProjectId = projectId,
                    IsRoot = false,
                    IsSpecial = false,
                    IsPublic = ctx.Request.isPublic,
                    IsActive = true,
                    Description = new MarkdownDbValue(ctx.Request.description),
                };
                ctx.MarkCreatedNow(group);
                ctx.Project.CharacterGroups.Add(group);
                return group;
            });

        // CharacterGroupId генерируется БД при SaveChanges — читаем уже после возврата из сервиса.
        return new CharacterGroupIdentification(projectId, group.CharacterGroupId);
    }

    public async Task EditCharacterGroup(CharacterGroupIdentification characterGroupId,
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> parentCharacterGroupIds,
        string description)
    {
        await projectPropsService.ChangeProjectProperties(
            characterGroupId.ProjectId,
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            (characterGroupId, name, isPublic, parentCharacterGroupIds, description),
            ctx =>
            {
                var (characterGroup, currentGroupInfo) = ctx.GetCharacterGroupForChange(ctx.Request.characterGroupId);

                var forbiddenParents = currentGroupInfo.AllChildGroupsIncludingThis;
                var cycleViolation = ServiceValidation.Required(ctx.Request.parentCharacterGroupIds)
                    .FirstOrDefault(p => forbiddenParents.Contains(p));
                if (cycleViolation is not null)
                {
                    throw new ArgumentException($"Группа {cycleViolation.CharacterGroupId} является потомком редактируемой группы и не может быть её родителем.");
                }

                characterGroup.CharacterGroupName = ServiceValidation.Required(ctx.Request.name);
                characterGroup.IsPublic = ctx.Request.isPublic;
                characterGroup.ParentCharacterGroupIds =
                    ctx.ProjectInfo.ValidateCharacterGroupList(
                        ServiceValidation.Required(ctx.Request.parentCharacterGroupIds),
                        ensureNotSpecial: true);
                characterGroup.Description = new MarkdownDbValue(ctx.Request.description);
            });
    }

    public async Task DeleteCharacterGroup(CharacterGroupIdentification characterGroupId)
    {
        await projectPropsService.ChangeProjectProperties(
            characterGroupId.ProjectId,
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            characterGroupId,
            ctx =>
            {
                var (characterGroup, _) = ctx.GetCharacterGroupForChange(ctx.Request);

                foreach (var character in characterGroup.Characters.Where(ch => ch.IsActive))
                {
                    if (character.ParentCharacterGroupIds.Except([ctx.Request.CharacterGroupId]).Any())
                    {
                        continue;
                    }

                    character.ParentCharacterGroupIds = character.ParentCharacterGroupIds
                        .Union(characterGroup.ParentCharacterGroupIds).ToArray();
                }

                foreach (var childGroup in characterGroup.ChildGroups.Where(ch => ch.IsActive))
                {
                    if (childGroup.ParentCharacterGroupIds.Except([ctx.Request.CharacterGroupId]).Any())
                    {
                        continue;
                    }

                    childGroup.ParentCharacterGroupIds = childGroup.ParentCharacterGroupIds
                        .Union(characterGroup.ParentCharacterGroupIds).ToArray();
                }

                if (characterGroup.CanBePermanentlyDeleted)
                {
                    characterGroup.DirectlyRelatedPlotElements.CleanLinksList();
                }

                _ = ctx.SmartDelete(characterGroup);
            });
    }

    public async Task MoveCharacterGroup(CharacterGroupIdentification characterGroupId,
        CharacterGroupIdentification parentCharacterGroupId,
        short direction)
    {
        await projectPropsService.ChangeProjectProperties(
            parentCharacterGroupId.ProjectId,
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            (characterGroupId, parentCharacterGroupId, direction),
            ctx =>
            {
                var (parentCharacterGroup, parentGroupInfo) = ctx.GetCharacterGroupForChange(ctx.Request.parentCharacterGroupId);

                var thisCharacterGroup =
                    parentCharacterGroup.ChildGroups.Single(i =>
                        i.CharacterGroupId == ctx.Request.characterGroupId.CharacterGroupId);

                parentCharacterGroup.ChildGroupsOrdering = parentCharacterGroup
                    .GetCharacterGroupsContainer().Move(thisCharacterGroup, ctx.Request.direction).GetStoredOrder();
            });
    }

    public async Task<IReadOnlyList<CharacterIdentification>> MoveCharacterAfter(
        CharacterGroupIdentification parentCharacterGroupId,
        CharacterIdentification characterId,
        CharacterIdentification? afterCharacterId)
    {
        return await projectPropsService.ChangeProjectProperties(
            parentCharacterGroupId.ProjectId,
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            (parentCharacterGroupId, characterId, afterCharacterId),
            ctx =>
            {
                var (parentCharacterGroup, _) = ctx.GetCharacterGroupForChange(ctx.Request.parentCharacterGroupId, allowSpecialToValue: true);

                var container = parentCharacterGroup.GetCharactersContainer()
                    .MoveAfter(ctx.Request.characterId.CharacterId, ctx.Request.afterCharacterId?.CharacterId);
                parentCharacterGroup.ChildCharactersOrdering = container.GetStoredOrder();

                return container.OrderedItems.Select(c => c.GetId()).ToList();
            });
    }
}
