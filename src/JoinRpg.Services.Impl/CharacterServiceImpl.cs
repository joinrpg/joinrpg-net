using System.Data.Entity.Validation;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl;

internal class CharacterServiceImpl : DbServiceImplBase, ICharacterService
{
    private readonly FieldSaveHelper fieldSaveHelper;
    private readonly IProjectMetadataRepository projectMetadataRepository;
    private readonly ILogger<CharacterServiceImpl> logger;

    public CharacterServiceImpl(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        FieldSaveHelper fieldSaveHelper,
        ICurrentUserAccessor currentUserAccessor,
        IProjectMetadataRepository projectMetadataRepository,
        ILogger<CharacterServiceImpl> logger) : base(unitOfWork, currentUserAccessor)
    {
        EmailService = emailService;
        this.fieldSaveHelper = fieldSaveHelper;
        this.projectMetadataRepository = projectMetadataRepository;
        this.logger = logger;
    }

    private IEmailService EmailService { get; }

    public async Task AddCharacter(AddCharacterRequest addCharacterRequest)
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
            ParentCharacterGroupIds =
                await ValidateCharacterGroupList(addCharacterRequest.ProjectId, Required(addCharacterRequest.ParentCharacterGroupIds)),
            ProjectId = addCharacterRequest.ProjectId,
            Project = project,
        };

        SetCharacterSettings(character, addCharacterRequest.CharacterTypeInfo);

        Create(character);
        MarkTreeModified(project);

        //TODO we do not send message for creating character
        _ = fieldSaveHelper.SaveCharacterFields(CurrentUserId,
            character,
            addCharacterRequest.FieldValues,
            projectInfo);

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

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(editCharacterRequest.Id.ProjectId));

        SetCharacterSettings(character, editCharacterRequest.CharacterTypeInfo);

        character.ParentCharacterGroupIds = await ValidateCharacterGroupList(editCharacterRequest.Id.ProjectId,
            Required(editCharacterRequest.ParentCharacterGroupIds),
            ensureNotSpecial: true);
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
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        _ = character.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);

        _ = character.EnsureProjectActive();

        var changedFields = fieldSaveHelper.SaveCharacterFields(CurrentUserId,
            character,
            requestFieldValues,
            projectInfo);

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

    public async Task<int?> CreateSlotFromGroup(int projectId, int characterGroupId, string slotName, bool allowToChangeInactive)
    {
        var group = await LoadProjectSubEntityAsync<CharacterGroup>(projectId, characterGroupId);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));

        if (!allowToChangeInactive)
        {
            group.Project.EnsureProjectActive();
        }

        if (!IsCurrentUserAdmin)
        {
            group.Project
                .RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
        }

        var claims = group.Claims.ToList();

        var needToSaveClaims = claims.Any();
        var needToInitSlot = group.HaveDirectSlots && group.Project.Active;
        var needToClearSlot = group.HaveDirectSlots;

        logger.LogInformation("Group (Id={characterGroupId}, Name={characterGroupName}) is evaluated to convert to slot. Decision (SaveClaims: {needToSaveClaims}, InitSlot: {needToInitSlot}, ClearSlot: {needToClearSlot})",
            characterGroupId,
            group.CharacterGroupName,
            needToSaveClaims,
            needToInitSlot,
            needToClearSlot);

        if (!needToSaveClaims && !needToInitSlot && !needToClearSlot)
        {
            return null; // Do nothing
        }

        MarkTreeModified(group.Project);

        Character? character;
        if (needToSaveClaims || needToInitSlot)
        {

            var addCharacterRequest = new AddCharacterRequest(
                projectId,
                new[] { characterGroupId },
                CharacterTypeInfo.DefaultSlot(slotName),
                new Dictionary<int, string?>());

            character = new Character
            {
                ParentCharacterGroupIds =
                    await ValidateCharacterGroupList(addCharacterRequest.ProjectId, Required(addCharacterRequest.ParentCharacterGroupIds)),
                ProjectId = addCharacterRequest.ProjectId,
                Project = group.Project,
            };

            SetCharacterSettings(character, addCharacterRequest.CharacterTypeInfo);

            Create(character);


            //TODO we do not send message for creating character
            _ = fieldSaveHelper.SaveCharacterFields(CurrentUserId,
                character,
                addCharacterRequest.FieldValues, projectInfo);

            if (needToInitSlot)
            {
                // Move limit to character
                character.CharacterSlotLimit = group.DirectSlotsUnlimited ? null : group.AvaiableDirectSlots;
                character.IsActive = true;
            }
            else
            {
                character.CharacterSlotLimit = 0;
                character.IsActive = claims.Any(c => c.IsPending); // if there is some alive claim
            }

            // Move claims from group to character

            foreach (var claim in claims)
            {
                claim.CharacterGroupId = null;
                claim.Character = character;
            }
        }
        else
        {
            character = null;
        }

        //Remove direct claim settings for group 
        group.HaveDirectSlots = false;
        group.AvaiableDirectSlots = 0;

        await UnitOfWork.SaveChangesAsync();

        return character?.CharacterId;
    }
}
