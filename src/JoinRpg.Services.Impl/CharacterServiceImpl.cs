using System.Data.Entity.Validation;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

internal class CharacterServiceImpl : DbServiceImplBase, ICharacterService
{
    public CharacterServiceImpl(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IFieldDefaultValueGenerator fieldDefaultValueGenerator,
        ICurrentUserAccessor currentUserAccessor
        ) : base(unitOfWork, currentUserAccessor)
    {
        EmailService = emailService;
        FieldDefaultValueGenerator = fieldDefaultValueGenerator;
    }

    private IEmailService EmailService { get; }
    private IFieldDefaultValueGenerator FieldDefaultValueGenerator { get; }

    public async Task AddCharacter(AddCharacterRequest addCharacterRequest)
    {
        var project = await ProjectRepository.GetProjectAsync(addCharacterRequest.ProjectId);

        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
        _ = project.EnsureProjectActive();

        var character = new Character
        {
            ParentCharacterGroupIds =
                await ValidateCharacterGroupList(addCharacterRequest.ProjectId, Required(addCharacterRequest.ParentCharacterGroupIds)),
            ProjectId = addCharacterRequest.ProjectId,
            Project = project,
        };

        SetCharacterSettings(character, addCharacterRequest.CharacterTypeInfo);

        Create(character);
        MarkTreeModified(project);

        //TODO we do not send message for creating character
        _ = FieldSaveHelper.SaveCharacterFields(CurrentUserId,
            character,
            addCharacterRequest.FieldValues,
            FieldDefaultValueGenerator);

        await UnitOfWork.SaveChangesAsync();
    }

    private static void SetCharacterSettings(Character character, CharacterTypeInfo characterTypeInfo)
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
            && character.Project.Details.CharacterNameField is null)
        {
            character.CharacterName = Required(characterTypeInfo.SlotName);
        }

        character.IsActive = true;
    }

    public async Task EditCharacter(EditCharacterRequest editCharacterRequest)
    {
        var character = await LoadCharacter(editCharacterRequest.Id);

        SetCharacterSettings(character, editCharacterRequest.CharacterTypeInfo);

        character.ParentCharacterGroupIds = await ValidateCharacterGroupList(editCharacterRequest.Id.ProjectId,
            Required(editCharacterRequest.ParentCharacterGroupIds),
            ensureNotSpecial: true);
        var changedFields = FieldSaveHelper.SaveCharacterFields(CurrentUserId,
            character,
            editCharacterRequest.FieldValues,
            FieldDefaultValueGenerator);

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
            await EmailService.Email(email);
        }
    }

    public async Task DeleteCharacter(DeleteCharacterRequest deleteCharacterRequest)
    {
        Character character = await LoadCharacter(deleteCharacterRequest.Id);

        if (character.HasActiveClaims())
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

        return character.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles).EnsureProjectActive();
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

    public async Task SetFields(int projectId, int characterId, Dictionary<int, string?> requestFieldValues)
    {
        var character = await LoadProjectSubEntityAsync<Character>(projectId, characterId);
        _ = character.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);

        _ = character.EnsureProjectActive();

        var changedFields = FieldSaveHelper.SaveCharacterFields(CurrentUserId,
            character,
            requestFieldValues,
            FieldDefaultValueGenerator);

        MarkChanged(character);

        FieldsChangedEmail? email = null;

        if (changedFields.Any())
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
            await EmailService.Email(email);
        }
    }

    public async Task<int> CreateSlotFromGroup(int projectId, int characterGroupId, string slotName)
    {
        var group = await LoadProjectSubEntityAsync<CharacterGroup>(projectId, characterGroupId);

        group.Project
            .RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles)
            .EnsureProjectActive();

        var fields = new Dictionary<int, string?>();
        var addCharacterRequest = new AddCharacterRequest(
            projectId,
            new[] { characterGroupId },
            CharacterTypeInfo.DefaultSlot(slotName),
            fields);

        var character = new Character
        {
            ParentCharacterGroupIds =
                await ValidateCharacterGroupList(addCharacterRequest.ProjectId, Required(addCharacterRequest.ParentCharacterGroupIds)),
            ProjectId = addCharacterRequest.ProjectId,
            Project = group.Project,
        };

        SetCharacterSettings(character, addCharacterRequest.CharacterTypeInfo);

        Create(character);
        MarkTreeModified(group.Project);

        //TODO we do not send message for creating character
        _ = FieldSaveHelper.SaveCharacterFields(CurrentUserId,
            character,
            addCharacterRequest.FieldValues,
            FieldDefaultValueGenerator);

        // Move limit to character
        character.CharacterSlotLimit = group.DirectSlotsUnlimited ? null : group.AvaiableDirectSlots;

        //Remove direct claim settings for groups
        group.HaveDirectSlots = false;
        group.AvaiableDirectSlots = 0;

        // Move claims from group to character
        foreach (var claim in group.Claims.ToList())
        {
            claim.CharacterGroupId = null;
            claim.Character = character;
        }

        await UnitOfWork.SaveChangesAsync();

        return character.CharacterId;
    }
}
