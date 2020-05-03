using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
    [UsedImplicitly]
    internal class ProjectService : DbServiceImplBase, IProjectService
    {


        public ProjectService(IUnitOfWork unitOfWork, ICurrentUserAccessor currentUserAccessor) : base(unitOfWork, currentUserAccessor)
        {
        }

        public async Task<Project> AddProject(CreateProjectRequest request)
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
                ProjectName = Required(request.ProjectName),
                CharacterGroups = new List<CharacterGroup>()
                {
                    rootGroup,
                },
                ProjectAcls = new List<ProjectAcl>()
                {
                    ProjectAcl.CreateRootAcl(CurrentUserId, isOwner: true),
                },
                Details = new ProjectDetails()
                {
                    CharacterNameLegacyMode = false,
                },
                ProjectFields = new List<ProjectField>(),
            };
            MarkTreeModified(project);
            ConfigureProjectDefaults(project, request.ProjectType);

            UnitOfWork.GetDbSet<Project>().Add(project);
            await UnitOfWork.SaveChangesAsync();
            return project;
        }

        private static void ConfigureProjectDefaults(Project project, ProjectTypeDto projectType)
        {
            var initialFieldId = -1000;
            ProjectField CreateField(string name, ProjectFieldType type)
            {
                var field = new ProjectField()
                {
                    CanPlayerEdit = false,
                    CanPlayerView = true,
                    FieldBoundTo = FieldBoundTo.Character,
                    FieldName = name,
                    FieldType = type,
                    IsActive = true,
                    IsPublic = true,
                    MandatoryStatus = MandatoryStatus.Optional,
                    IncludeInPrint = false,
                    ValidForNpc = true,
                    ProjectFieldId = initialFieldId,
                    Description = new MarkdownString(""),
                    MasterDescription = new MarkdownString(""),
                };
                project.ProjectFields.Add(field);
                initialFieldId++;
                return field;
            }

            switch (projectType)
            {
                case ProjectTypeDto.Larp:
                    project.Details.CharacterNameField = CreateField("Имя персонажа", ProjectFieldType.String);
                    project.Details.CharacterNameField.MandatoryStatus = MandatoryStatus.Required;

                    project.Details.CharacterDescription = CreateField("Описание персонажа", ProjectFieldType.Text);
                    break;
                case ProjectTypeDto.Convention:
                    project.Details.AutoAcceptClaims = true;
                    project.Details.EnableAccommodation = true;
                    project.Details.CharacterNameField = null;
                    project.Details.CharacterDescription = null;
                    break;
                case ProjectTypeDto.ConventionProgram:
                    project.Details.EnableManyCharacters = true;

                    project.Details.CharacterNameField = CreateField("Название мероприятия", ProjectFieldType.String);
                    project.Details.CharacterNameField.MandatoryStatus = MandatoryStatus.Required;
                    project.Details.CharacterNameField.CanPlayerEdit = true;

                    project.Details.CharacterDescription = CreateField("Описание мероприятия", ProjectFieldType.Text);
                    project.Details.CharacterDescription.CanPlayerEdit = true;

                    var timeField = CreateField("Время проведения мероприятия", ProjectFieldType.MultiSelect);
                    timeField.MasterDescription = new MarkdownString("Здесь вы можете указать, когда проводится мероприятие. Настройте в свойствах поля возможное время проведения");

                    var roomField = CreateField("Место проведения мероприятия", ProjectFieldType.MultiSelect);
                    roomField.MasterDescription = new MarkdownString("Здесь вы можете указать, где проводится мероприятие. Настройте в свойствах поля возможное время проведения");

                    project.Details.ScheduleSettings = new ProjectScheduleSettings
                    {
                        RoomField = roomField,
                        TimeSlotField = timeField,
                    };

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(projectType));
            }
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

        public async Task GrantAccessAsAdmin(int projectId)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);
            if (!IsCurrentUserAdmin)
            {
                throw new NoAccessToProjectException(project, CurrentUserId);
            }
            

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
                !user.Auth.IsAdmin)
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
                if (!user.Auth.IsAdmin)
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
            UnitOfWork.GetDbSet<UserSubscription>()
                .RemoveRange(UnitOfWork.GetDbSet<UserSubscription>()
                             .Where(x => x.UserId == userId && x.ProjectId == projectId));

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


    }
}

