using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl
{
    [UsedImplicitly]
    internal class ProjectService : DbServiceImplBase, IProjectService
    {
        private IEmailService EmailService { get; }
        private IFieldDefaultValueGenerator FieldDefaultValueGenerator { get; }

        public ProjectService(IUnitOfWork unitOfWork,
            IEmailService emailService,
            IFieldDefaultValueGenerator fieldDefaultValueGenerator) : base(unitOfWork)
        {
            FieldDefaultValueGenerator = fieldDefaultValueGenerator;
            EmailService = emailService;
        }

        public async Task<Project> AddProject(string projectName)
        {
            var rootGroup = new CharacterGroup()
            {
                IsPublic = true,
                IsRoot = true,
                //TODO[Localize]
                CharacterGroupName = "Все роли",
                IsActive = true,
                ResponsibleMasterUserId = CurrentUserId,
                HaveDirectSlots = true,
                AvaiableDirectSlots = -1,
            };
            MarkCreatedNow(rootGroup);
            var project = new Project()
            {
                Active = true,
                IsAcceptingClaims = false,
                CreatedDate = Now,
                ProjectName = Required(projectName),
                CharacterGroups = new List<CharacterGroup>()
                {
                    rootGroup,
                },
                ProjectAcls = new List<ProjectAcl>()
                {
                    ProjectAcl.CreateRootAcl(CurrentUserId, isOwner: true),
                },
                Details = new ProjectDetails(),
            };
            MarkTreeModified(project);
            UnitOfWork.GetDbSet<Project>().Add(project);
            await UnitOfWork.SaveChangesAsync();
            return project;
        }


        public async Task AddCharacterGroup(int projectId,
            string name,
            bool isPublic,
            IReadOnlyCollection<int> parentCharacterGroupIds,
            string description,
            bool haveDirectSlotsForSave,
            int directSlotsForSave,
            int? responsibleMasterId)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);

            if (responsibleMasterId != null &&
                project.ProjectAcls.All(acl => acl.UserId != responsibleMasterId))
            {
                //TODO: Move this check into ChGroup validation
                throw new Exception("No such master");
            }

            project.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
            project.EnsureProjectActive();

            Create(new CharacterGroup()
            {
                AvaiableDirectSlots = directSlotsForSave,
                HaveDirectSlots = haveDirectSlotsForSave,
                CharacterGroupName = Required(name),
                ParentCharacterGroupIds =
                    await ValidateCharacterGroupList(projectId,
                        Required(() => parentCharacterGroupIds)),
                ProjectId = projectId,
                IsRoot = false,
                IsSpecial = false,
                IsPublic = isPublic,
                IsActive = true,
                Description = new MarkdownString(description),
                ResponsibleMasterUserId = responsibleMasterId,
            });

            MarkTreeModified(project);

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task AddCharacter(AddCharacterRequest addCharacterRequest)
        {
            var project = await ProjectRepository.GetProjectAsync(addCharacterRequest.ProjectId);

            project.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
            project.EnsureProjectActive();

            var character = new Character
            {
                CharacterName = Required(addCharacterRequest.Name),
                ParentCharacterGroupIds =
                    await ValidateCharacterGroupList(addCharacterRequest.ProjectId, Required(addCharacterRequest.ParentCharacterGroupIds)),
                ProjectId = addCharacterRequest.ProjectId,
                IsPublic = addCharacterRequest.IsPublic,
                IsActive = true,
                IsAcceptingClaims = addCharacterRequest.IsAcceptingClaims,
                Description = new MarkdownString(addCharacterRequest.Description),
                HidePlayerForCharacter = addCharacterRequest.HidePlayerForCharacter,
                IsHot = addCharacterRequest.IsHot,
            };
            Create(character);
            MarkTreeModified(project);

            // ReSharper disable once MustUseReturnValue
            //TODO we do not send message for creating character
            FieldSaveHelper.SaveCharacterFields(CurrentUserId,
                character,
                addCharacterRequest.FieldValues,
                FieldDefaultValueGenerator);

            await UnitOfWork.SaveChangesAsync();
        }

        //TODO: move character operations to a separate service.
        public async Task EditCharacter(int currentUserId,
            int characterId,
            int projectId,
            string name,
            bool isPublic,
            IReadOnlyCollection<int> parentCharacterGroupIds,
            bool isAcceptingClaims,
            string contents,
            bool hidePlayerForCharacter,
            IReadOnlyDictionary<int, string> characterFields,
            bool isHot)
        {
            var character = await LoadProjectSubEntityAsync<Character>(projectId, characterId);
            character.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);

            character.EnsureProjectActive();

            var changedAttributes = new Dictionary<string, PreviousAndNewValue>();

            changedAttributes.Add("Имя персонажа",
                new PreviousAndNewValue(name, character.CharacterName.Trim()));
            character.CharacterName = name.Trim();

            character.IsAcceptingClaims = isAcceptingClaims;
            character.IsPublic = isPublic;

            var newDescription = new MarkdownString(contents);

            changedAttributes.Add("Описание персонажа",
                new PreviousAndNewValue(newDescription, character.Description));
            character.Description = newDescription;

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

        public async Task MoveCharacterGroup(int currentUserId,
            int projectId,
            int charactergroupId,
            int parentCharacterGroupId,
            short direction)
        {
            var parentCharacterGroup =
                await ProjectRepository.LoadGroupWithChildsAsync(projectId, parentCharacterGroupId);
            parentCharacterGroup.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);
            parentCharacterGroup.EnsureProjectActive();

            var thisCharacterGroup =
                parentCharacterGroup.ChildGroups.Single(i =>
                    i.CharacterGroupId == charactergroupId);

            parentCharacterGroup.ChildGroupsOrdering = parentCharacterGroup
                .GetCharacterGroupsContainer().Move(thisCharacterGroup, direction).GetStoredOrder();
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

        public async Task CloseProject(int projectId, int currentUserId, bool publishPlot)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);

            var user = await UserRepository.GetById(currentUserId);
            RequestProjectAdminAccess(project, user);

            project.Active = false;
            project.IsAcceptingClaims = false;
            project.Details.PublishPlot = publishPlot;

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task SetCheckInOptions(int projectId,
            bool checkInProgress,
            bool enableCheckInModule,
            bool modelAllowSecondRoles)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);
            project.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeProjectProperties);

            project.Details.CheckInProgress = checkInProgress && enableCheckInModule;
            project.Details.EnableCheckInModule = enableCheckInModule;
            project.Details.AllowSecondRoles = modelAllowSecondRoles && enableCheckInModule;
            await UnitOfWork.SaveChangesAsync();
        }

        [PrincipalPermission(SecurityAction.Demand, Role = Security.AdminRoleName)]
        public async Task GrantAccessAsAdmin(int projectId)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);

            var acl = project.ProjectAcls.SingleOrDefault(a => a.UserId == CurrentUserId);
            if (acl == null)
            {
                project.ProjectAcls.Add(ProjectAcl.CreateRootAcl(CurrentUserId));
            }

            await UnitOfWork.SaveChangesAsync();
        }

        private static void RequestProjectAdminAccess(Project project, User user)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (!project.HasMasterAccess(user.UserId, acl => acl.CanChangeProjectProperties) &&
                user.Auth?.IsAdmin != true)
            {
                throw new NoAccessToProjectException(project,
                    user.UserId,
                    acl => acl.CanChangeProjectProperties);
            }
        }

        public async Task EditCharacterGroup(int projectId,
            int currentUserId,
            int characterGroupId,
            string name,
            bool isPublic,
            IReadOnlyCollection<int> parentCharacterGroupIds,
            string description,
            bool haveDirectSlots,
            int directSlots,
            int? responsibleMasterId)
        {
            var characterGroup =
                (await ProjectRepository.GetGroupAsync(projectId, characterGroupId))
                .RequestMasterAccess(currentUserId, acl => acl.CanEditRoles)
                .EnsureProjectActive();

            if (!characterGroup.IsRoot
            ) //We shoud not edit root group, except of possibility of direct claims here
            {
                characterGroup.CharacterGroupName = Required(name);
                characterGroup.IsPublic = isPublic;
                characterGroup.ParentCharacterGroupIds =
                    await ValidateCharacterGroupList(projectId,
                        Required(parentCharacterGroupIds),
                        ensureNotSpecial: true);
                characterGroup.Description = new MarkdownString(description);
            }

            if (responsibleMasterId != null &&
                characterGroup.Project.ProjectAcls.All(acl => acl.UserId != responsibleMasterId))
            {
                throw new Exception("No such master");
            }

            characterGroup.ResponsibleMasterUserId = responsibleMasterId;
            characterGroup.AvaiableDirectSlots = directSlots;
            characterGroup.HaveDirectSlots = haveDirectSlots;

            MarkTreeModified(characterGroup.Project); // Can be smarted than this
            MarkChanged(characterGroup);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task DeleteCharacterGroup(int projectId, int characterGroupId)
        {
            var characterGroup = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

            if (characterGroup == null) throw new DbEntityValidationException();

            if (characterGroup.HasActiveClaims())
            {
                throw new DbEntityValidationException();
            }

            characterGroup.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
            characterGroup.EnsureProjectActive();

            foreach (var character in characterGroup.Characters.Where(ch => ch.IsActive))
            {
                if (character.ParentCharacterGroupIds.Except(new[] {characterGroupId}).Any())
                {
                    continue;
                }

                character.ParentCharacterGroupIds = character.ParentCharacterGroupIds
                    .Union(characterGroup.ParentCharacterGroupIds).ToArray();
            }

            foreach (var character in characterGroup.ChildGroups.Where(ch => ch.IsActive))
            {
                if (character.ParentCharacterGroupIds.Except(new[] { characterGroupId }).Any())
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
                characterGroup.DirectlyRelatedPlotFolders.CleanLinksList();
                characterGroup.DirectlyRelatedPlotElements.CleanLinksList();
            }

            SmartDelete(characterGroup);

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task EditProject(EditProjectRequest request)
        {
            var project = await ProjectRepository.GetProjectAsync(request.ProjectId);

            project.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeProjectProperties);

            project.Details.ClaimApplyRules = new MarkdownString(request.ClaimApplyRules);
            project.Details.ProjectAnnounce = new MarkdownString(request.ProjectAnnounce);
            project.Details.EnableManyCharacters = request.MultipleCharacters;
            project.Details.PublishPlot = request.PublishPlot && !project.Active;
            project.ProjectName = Required(request.ProjectName);
            project.IsAcceptingClaims = request.IsAcceptingClaims && project.Active;

            project.Details.GenerateCharacterNamesFromPlayer = request.GenerateCharacterNamesFromPlayer;
            project.Details.AutoAcceptClaims = request.AutoAcceptClaims;
            project.Details.EnableAccommodation = request.IsAccommodationEnabled;

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task GrantAccess(GrantAccessRequest grantAccessRequest)
        {
            var project = await ProjectRepository.GetProjectAsync(grantAccessRequest.ProjectId);
            if (!project.HasMasterAccess(CurrentUserId, a => a.CanGrantRights))
            {
                var user = await UserRepository.GetById(CurrentUserId);
                if (!user.Auth?.IsAdmin == true)
                {
                    project.RequestMasterAccess(CurrentUserId, a => a.CanGrantRights);
                }
            }

            project.EnsureProjectActive();

            var acl = project.ProjectAcls.SingleOrDefault(a => a.UserId == grantAccessRequest.UserId);
            if (acl == null)
            {
                acl = new ProjectAcl
                {
                    ProjectId = project.ProjectId,
                    UserId = grantAccessRequest.UserId,
                };
                project.ProjectAcls.Add(acl);
            }

            SetRightsFromRequest(grantAccessRequest, acl);

            await UnitOfWork.SaveChangesAsync();
        }

        private static void SetRightsFromRequest(AccessRequestBase grantAccessRequest, ProjectAcl acl)
        {
            acl.CanGrantRights = grantAccessRequest.CanGrantRights;
            acl.CanChangeFields = grantAccessRequest.CanChangeFields;
            acl.CanChangeProjectProperties = grantAccessRequest.CanChangeProjectProperties;
            acl.CanManageClaims = grantAccessRequest.CanManageClaims;
            acl.CanEditRoles = grantAccessRequest.CanEditRoles;
            acl.CanManageMoney = grantAccessRequest.CanManageMoney;
            acl.CanSendMassMails = grantAccessRequest.CanSendMassMails;
            acl.CanManagePlots = grantAccessRequest.CanManagePlots;
            acl.CanManageAccommodation = grantAccessRequest.CanManageAccommodation &&
                                         acl.Project.Details.EnableAccommodation;
            acl.CanSetPlayersAccommodations = grantAccessRequest.CanSetPlayersAccommodations &&
                                              acl.Project.Details.EnableAccommodation;
        }

        public async Task RemoveAccess(int projectId, int userId, int? newResponsibleMasterId)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);
            project.RequestMasterAccess(CurrentUserId, a => a.CanGrantRights);

            if (!project.ProjectAcls.Any(a => a.CanGrantRights && a.UserId != userId))
            {
                throw new DbEntityValidationException();
            }

            var acl = project.ProjectAcls.Single(
                a => a.ProjectId == projectId && a.UserId == userId);

            var respFor = await ProjectRepository.GetGroupsWithResponsible(projectId);
            if (respFor.Any(item => item.ResponsibleMasterUserId == userId))
            {
                throw new MasterHasResponsibleException(acl);
            }

            var claims =
                await ClaimsRepository.GetClaimsForMaster(projectId,
                    acl.UserId,
                    ClaimStatusSpec.Any);

            if (claims.Any())
            {
                if (newResponsibleMasterId == null)
                {
                    throw new MasterHasResponsibleException(acl);
                }

                project.RequestMasterAccess((int)newResponsibleMasterId);

                foreach (var claim in claims)
                {
                    claim.ResponsibleMasterUserId = newResponsibleMasterId;
                }
            }

            if (acl.IsOwner)
            {
                if (acl.UserId == CurrentUserId)
                {
                    // if owner removing himself, assign "random" owner
                    project.ProjectAcls.OrderBy(a => a.UserId).First().IsOwner = true;
                }
                else
                {
                    //who kills the king, becomes one
                    project.ProjectAcls.Single(a => a.UserId == CurrentUserId).IsOwner = true;
                }
            }

            UnitOfWork.GetDbSet<ProjectAcl>().Remove(acl);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task ChangeAccess(ChangeAccessRequest changeAccessRequest)
        {
            var project = await ProjectRepository.GetProjectAsync(changeAccessRequest.ProjectId);
            project.RequestMasterAccess(CurrentUserId, a => a.CanGrantRights);

            var acl = project.ProjectAcls.Single(
                a => a.ProjectId == changeAccessRequest.ProjectId && a.UserId == changeAccessRequest.UserId);
            SetRightsFromRequest(changeAccessRequest, acl);

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task UpdateSubscribeForGroup(SubscribeForGroupRequest request)
        {
            (await ProjectRepository.GetGroupAsync(request.ProjectId, request.CharacterGroupId))
                .RequestMasterAccess(CurrentUserId)
                .EnsureActive();

            var user = await UserRepository.GetWithSubscribe(CurrentUserId);
            var direct =
                user.Subscriptions.SingleOrDefault(s => s.CharacterGroupId == request.CharacterGroupId);

            if (request.AnySet())
            {
                if (direct == null)
                {
                    direct = new UserSubscription()
                    {
                        UserId = CurrentUserId,
                        CharacterGroupId = request.CharacterGroupId,
                        ProjectId = request.ProjectId,
                    };
                    user.Subscriptions.Add(direct);
                }

                direct.AssignFrom(request);
            }
            else
            {
                if (direct != null)
                {
                    UnitOfWork.GetDbSet<UserSubscription>().Remove(direct);
                }
            }

            await UnitOfWork.SaveChangesAsync();
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
    }
}

