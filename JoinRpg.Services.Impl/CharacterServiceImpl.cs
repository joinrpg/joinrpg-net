using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl
{
    internal  class CharacterServiceImpl : DbServiceImplBase, ICharacterService
    {
        public CharacterServiceImpl(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IFieldDefaultValueGenerator fieldDefaultValueGenerator) : base(unitOfWork)
        {
            EmailService = emailService;
            FieldDefaultValueGenerator = fieldDefaultValueGenerator;
        }

        private IEmailService EmailService { get; }
        private IFieldDefaultValueGenerator FieldDefaultValueGenerator { get; }

        public async Task AddCharacter(AddCharacterRequest addCharacterRequest)
        {
            var project = await ProjectRepository.GetProjectAsync(addCharacterRequest.ProjectId);

            project.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
            project.EnsureProjectActive();

            var character = new Character
            {
                ParentCharacterGroupIds =
                    await ValidateCharacterGroupList(addCharacterRequest.ProjectId, Required(addCharacterRequest.ParentCharacterGroupIds)),
                ProjectId = addCharacterRequest.ProjectId,
                IsPublic = addCharacterRequest.IsPublic,
                IsActive = true,
                IsAcceptingClaims = addCharacterRequest.IsAcceptingClaims,
                HidePlayerForCharacter = addCharacterRequest.HidePlayerForCharacter,
                IsHot = addCharacterRequest.IsHot,
            };
            Create(character);
            MarkTreeModified(project);

            if (project.Details.CharacterNameLegacyMode)
            {
                character.CharacterName = Required(addCharacterRequest.Name);
                // If not legacy mode, character name will be updated inside SaveCharacterFields(..)
            }

            // ReSharper disable once MustUseReturnValue
            //TODO we do not send message for creating character
            FieldSaveHelper.SaveCharacterFields(CurrentUserId,
                character,
                addCharacterRequest.FieldValues,
                FieldDefaultValueGenerator);

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task EditCharacter(int currentUserId,
            int characterId,
            int projectId,
            string name,
            bool isPublic,
            IReadOnlyCollection<int> parentCharacterGroupIds,
            bool isAcceptingClaims,
            bool hidePlayerForCharacter,
            IReadOnlyDictionary<int, string> characterFields,
            bool isHot)
        {
            var character = await LoadProjectSubEntityAsync<Character>(projectId, characterId);
            character.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);

            character.EnsureProjectActive();

            var changedAttributes = new Dictionary<string, PreviousAndNewValue>();

            if (character.Project.Details.CharacterNameLegacyMode)
            {
                changedAttributes.Add("Имя персонажа",
                    new PreviousAndNewValue(name, character.CharacterName.Trim()));
                character.CharacterName = name.Trim();
                // If not legacy mode, character name will be updated inside SaveCharacterFields(..)
            }

            character.IsAcceptingClaims = isAcceptingClaims;
            character.IsPublic = isPublic;

            character.HidePlayerForCharacter = hidePlayerForCharacter;
            character.IsHot = isHot;
            character.IsActive = true;

            character.ParentCharacterGroupIds = await ValidateCharacterGroupList(projectId,
                Required(parentCharacterGroupIds),
                ensureNotSpecial: true);
            var changedFields = FieldSaveHelper.SaveCharacterFields(currentUserId,
                character,
                characterFields,
                FieldDefaultValueGenerator);

            MarkChanged(character);
            MarkTreeModified(character.Project); //TODO: Can be smarter

            FieldsChangedEmail email = null;
            changedAttributes = changedAttributes
                .Where(attr => attr.Value.DisplayString != attr.Value.PreviousDisplayString)
                .ToDictionary(x => x.Key, x => x.Value);

            if (changedFields.Any() || changedAttributes.Any())
            {
                var user = await UserRepository.GetById(currentUserId);
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

        public async Task DeleteCharacter(int projectId, int characterId, int currentUserId)
        {
            var character = await CharactersRepository.GetCharacterAsync(projectId, characterId);

            character.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);
            character.EnsureProjectActive();

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

        public async Task MoveCharacter(int currentUserId,
            int projectId,
            int characterId,
            int parentCharacterGroupId,
            short direction)
        {
            var parentCharacterGroup =
                await ProjectRepository.LoadGroupWithChildsAsync(projectId, parentCharacterGroupId);
            parentCharacterGroup.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);
            parentCharacterGroup.EnsureProjectActive();

            var item = parentCharacterGroup.Characters.Single(i => i.CharacterId == characterId);

            parentCharacterGroup.ChildCharactersOrdering = parentCharacterGroup
                .GetCharactersContainer().Move(item, direction).GetStoredOrder();
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task SetFields(int projectId, int characterId, Dictionary<int, string> requestFieldValues)
        {
            var character = await LoadProjectSubEntityAsync<Character>(projectId, characterId);
            character.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);

            character.EnsureProjectActive();

            var changedFields = FieldSaveHelper.SaveCharacterFields(CurrentUserId,
                character,
                requestFieldValues,
                FieldDefaultValueGenerator);

            MarkChanged(character);

            FieldsChangedEmail email = null;

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
