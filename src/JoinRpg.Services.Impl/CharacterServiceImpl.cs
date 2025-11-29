using System.Data.Entity.Validation;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

internal class CharacterServiceImpl(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    FieldSaveHelper fieldSaveHelper,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository) : DbServiceImplBase(unitOfWork, currentUserAccessor), ICharacterService
{
    public async Task<CharacterIdentification> AddCharacter(AddCharacterRequest addCharacterRequest)
    {
        var project = await ProjectRepository.GetProjectWithFieldsAsync(addCharacterRequest.ProjectId);

        if (project is null)
        {
            throw new JoinRpgEntityNotFoundException(addCharacterRequest.ProjectId, "Project");
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(addCharacterRequest.ProjectId));

        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
        _ = project.EnsureProjectActive();

        var character = new Character
        {
            ParentCharacterGroupIds = await ValidateGroupListForCharacter(projectInfo, addCharacterRequest.ParentCharacterGroupIds),
            ProjectId = addCharacterRequest.ProjectId,
            Project = project,
        };

        SetCharacterSettings(character, addCharacterRequest.CharacterTypeInfo, projectInfo);

        Create(character);
        MarkTreeModified(project);

        //TODO we do not send message for creating character
        _ = fieldSaveHelper.SaveCharacterFields(CurrentUserId,
            character,
            addCharacterRequest.FieldValues,
            projectInfo);

        await UnitOfWork.SaveChangesAsync();

        return new CharacterIdentification(character.ProjectId, character.CharacterId);
    }

    private static void SetCharacterSettings(Character character, CharacterTypeInfo characterTypeInfo, ProjectInfo projectInfo)
    {
        if (character.Claims.Any(claim => claim.ClaimStatus.IsActive())
            && characterTypeInfo.CharacterType != character.CharacterType)
        {
            throw new Exception("Can't change type of character with active claims");
        }

        (character.CharacterType,
            character.IsHot,
            character.CharacterSlotLimit,
            character.IsAcceptingClaims,
            _,
            character.IsPublic,
            character.HidePlayerForCharacter) = characterTypeInfo;

        if (characterTypeInfo.CharacterType == CharacterType.Slot
            && projectInfo.CharacterNameField is null)
        {
            character.CharacterName = Required(characterTypeInfo.SlotName);
        }

        character.IsActive = true;
    }

    public async Task EditCharacter(EditCharacterRequest editCharacterRequest)
    {
        var character = await LoadCharacter(editCharacterRequest.Id);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(editCharacterRequest.Id.ProjectId);

        SetCharacterSettings(character, editCharacterRequest.CharacterTypeInfo, projectInfo);

        character.ParentCharacterGroupIds = await ValidateGroupListForCharacter(projectInfo, editCharacterRequest.ParentCharacterGroupIds);

        var changedFields = fieldSaveHelper.SaveCharacterFields(CurrentUserId,
            character,
            editCharacterRequest.FieldValues,
            projectInfo);

        MarkChanged(character);
        MarkTreeModified(character.Project); //TODO: Can be smarter

        FieldsChangedEmail? email = null;

        if (changedFields.Any())
        {
            var user = await GetCurrentUser();
            email = EmailHelpers.CreateFieldsEmail(
                character,
                s => s.FieldChange,
                user,
                changedFields);
        }

        await UnitOfWork.SaveChangesAsync();

        if (email != null)
        {
            await emailService.Email(email);
        }
    }

    public async Task DeleteCharacter(DeleteCharacterRequest deleteCharacterRequest)
    {
        Character character = await LoadCharacter(deleteCharacterRequest.Id);

        if (character.HasActiveClaims() || character.Project.Details.DefaultTemplateCharacter == character)
        {
            throw new DbEntityValidationException();
        }

        MarkTreeModified(character.Project);

        if (character.CanBePermanentlyDeleted)
        {
            character.DirectlyRelatedPlotElements.CleanLinksList();
        }

        character.IsActive = false;
        MarkChanged(character);
        await UnitOfWork.SaveChangesAsync();
    }

    private async Task<Character> LoadCharacter(CharacterIdentification moniker)
    {
        var character = await CharactersRepository.GetCharacterAsync(moniker.ProjectId, moniker.CharacterId);

        return character.RequestMasterAccess(CurrentUserId, Permission.CanEditRoles).EnsureProjectActive();
    }

    public async Task MoveCharacter(int currentUserId,
        int projectId,
        int characterId,
        int parentCharacterGroupId,
        short direction)
    {
        var parentCharacterGroup =
            await ProjectRepository.LoadGroupWithChildsAsync(projectId, parentCharacterGroupId);
        _ = parentCharacterGroup.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);
        _ = parentCharacterGroup.EnsureProjectActive();

        var item = parentCharacterGroup.Characters.Single(i => i.CharacterId == characterId);

        parentCharacterGroup.ChildCharactersOrdering = parentCharacterGroup
            .GetCharactersContainer().Move(item, direction).GetStoredOrder();
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task SetFields(CharacterIdentification characterId, Dictionary<int, string?> requestFieldValues)
    {
        var character = await LoadCharacter(characterId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(characterId.ProjectId);

        var changedFields = fieldSaveHelper.SaveCharacterFields(CurrentUserId,
            character,
            requestFieldValues,
            projectInfo);

        MarkChanged(character);

        FieldsChangedEmail? email = null;

        if (changedFields.Count != 0)
        {
            var user = await UserRepository.GetById(CurrentUserId);
            email = EmailHelpers.CreateFieldsEmail(
                character,
                s => s.FieldChange,
                user,
                changedFields);
        }

        await UnitOfWork.SaveChangesAsync();

        if (email != null)
        {
            await emailService.Email(email);
        }
    }

    private async Task<int[]> ValidateGroupListForCharacter(ProjectInfo projectInfo, IReadOnlyCollection<CharacterGroupIdentification> groupIds)
    {
        return projectInfo.AllowToSetGroups ?
                        await ValidateCharacterGroupList(projectInfo.ProjectId, Required(groupIds), ensureNotSpecial: true)
                        : [projectInfo.RootCharacterGroupId.CharacterGroupId];
    }
}
