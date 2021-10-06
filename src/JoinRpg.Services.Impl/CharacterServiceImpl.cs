using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl
{
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
                IsPublic = addCharacterRequest.IsPublic,
                IsActive = true,
                HidePlayerForCharacter = addCharacterRequest.HidePlayerForCharacter,
            };

            (character.CharacterType, character.IsHot, character.CharacterSlotLimit, character.IsAcceptingClaims) = addCharacterRequest.CharacterTypeInfo;

            Create(character);
            MarkTreeModified(project);

            if (project.Details.CharacterNameLegacyMode)
            {
                character.CharacterName = Required(addCharacterRequest.Name);
                // If not legacy mode, character name will be updated inside SaveCharacterFields(..)
            }

            // ReSharper disable once MustUseReturnValue
            //TODO we do not send message for creating character
            _ = FieldSaveHelper.SaveCharacterFields(CurrentUserId,
                character,
                addCharacterRequest.FieldValues,
                FieldDefaultValueGenerator);

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task EditCharacter(EditCharacterRequest editCharacterRequest)
        {
            var character = await LoadCharacter(editCharacterRequest.Id);

            var changedAttributes = new Dictionary<string, PreviousAndNewValue>();

            if (character.Project.Details.CharacterNameLegacyMode)
            {
                var name = Required(editCharacterRequest.Name);

                changedAttributes.Add("Имя персонажа", new PreviousAndNewValue(name, character.CharacterName.Trim()));
                character.CharacterName = name;
                // If not legacy mode, character name will be updated inside SaveCharacterFields(..)
            }

            (character.CharacterType, character.IsHot, character.CharacterSlotLimit, character.IsAcceptingClaims) = editCharacterRequest.CharacterTypeInfo;

            character.IsPublic = editCharacterRequest.IsPublic;

            character.HidePlayerForCharacter = editCharacterRequest.HidePlayerForCharacter;
            character.IsActive = true;

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
            changedAttributes = changedAttributes
                .Where(attr => attr.Value.DisplayString != attr.Value.PreviousDisplayString)
                .ToDictionary(x => x.Key, x => x.Value);

            if (changedFields.Any() || changedAttributes.Any())
            {
                var user = await GetCurrentUser();
                email = EmailHelpers.CreateFieldsEmail(
                    character,
                    s => s.FieldChange,
                    user,
                    changedFields,
                    changedAttributes);
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
    }
}
