using System.Data.Entity.Validation;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl;

internal class ProjectService(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IMasterEmailService masterEmailService,
    ILogger<ProjectService> logger) : DbServiceImplBase(unitOfWork, currentUserAccessor), IProjectService
{
    public async Task<Project> AddProject(ProjectName projectName, string rootCharacterGroupName)
    {
        var rootGroup = new CharacterGroup()
        {
            IsPublic = true,
            IsRoot = true,
            CharacterGroupName = rootCharacterGroupName,
            IsActive = true,
            ResponsibleMasterUserId = CurrentUserId,
            HaveDirectSlots = false,
            AvaiableDirectSlots = 0,
        };
        MarkCreatedNow(rootGroup);

        var project = new Project()
        {
            Active = true,
            IsAcceptingClaims = false,
            CreatedDate = Now,
            ProjectName = projectName,
            CharacterGroups = [rootGroup,],
            ProjectAcls = [ProjectAcl.CreateRootAcl(CurrentUserId, isOwner: true),],
            Details = new ProjectDetails(),
            ProjectFields = [],
        };
        MarkTreeModified(project);

        _ = UnitOfWork.GetDbSet<Project>().Add(project);
        await UnitOfWork.SaveChangesAsync();
        return project;
    }



    public async Task AddCharacterGroup(int projectId,
        string name,
        bool isPublic,
        IReadOnlyCollection<int> parentCharacterGroupIds,
        string description)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);

        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
        _ = project.EnsureProjectActive();

        Create(new CharacterGroup()
        {
            AvaiableDirectSlots = 0,
            HaveDirectSlots = false,
            CharacterGroupName = Required(name),
            ParentCharacterGroupIds =
                await ValidateCharacterGroupList(projectId,
                    Required(parentCharacterGroupIds)),
            ProjectId = projectId,
            IsRoot = false,
            IsSpecial = false,
            IsPublic = isPublic,
            IsActive = true,
            Description = new MarkdownString(description),
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
        _ = parentCharacterGroup.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);
        _ = parentCharacterGroup.EnsureProjectActive();

        var thisCharacterGroup =
            parentCharacterGroup.ChildGroups.Single(i =>
                i.CharacterGroupId == charactergroupId);

        parentCharacterGroup.ChildGroupsOrdering = parentCharacterGroup
            .GetCharacterGroupsContainer().Move(thisCharacterGroup, direction).GetStoredOrder();
        await UnitOfWork.SaveChangesAsync();
    }



    public async Task CloseProject(ProjectIdentification projectId, bool publishPlot)
    {
        var project = RequestProjectAdminAccess(await ProjectRepository.GetProjectAsync(projectId));

        await CloseProjectImpl(project, publishPlot);

        await masterEmailService.EmailProjectClosed(new ProjectClosedMail()
        {
            ProjectId = projectId,
            Initiator = new UserIdentification(CurrentUserId),
        });
    }

    public async Task CloseProjectAsStale(ProjectIdentification projectId, DateOnly lastActiveDate)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);

        await CloseProjectImpl(project, false);

        await UnitOfWork.SaveChangesAsync();

        await masterEmailService.EmailProjectClosedStale(new ProjectClosedStaleMail()
        {
            ProjectId = projectId,
            LastActiveDate = lastActiveDate,
        });

        logger.LogInformation("Project {project} is closed as stale project.", projectId);
    }

    private async Task CloseProjectImpl(Project project, bool publishPlot)
    {
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
        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeProjectProperties);

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

    private Project RequestProjectAdminAccess(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (IsCurrentUserAdmin)
        {
            return project;
        }

        return project.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeProjectProperties);
    }

    public async Task EditCharacterGroup(int projectId,
        int currentUserId,
        int characterGroupId,
        string name,
        bool isPublic,
        IReadOnlyCollection<int> parentCharacterGroupIds,
        string description,
        bool haveDirectSlots,
        int directSlots)
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
        characterGroup.AvaiableDirectSlots = directSlots;
        characterGroup.HaveDirectSlots = haveDirectSlots;

        MarkTreeModified(characterGroup.Project); // Can be smarted than this
        MarkChanged(characterGroup);
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteCharacterGroup(int projectId, int characterGroupId)
    {
        var characterGroup = await ProjectRepository.GetGroupAsync(projectId, characterGroupId) ?? throw new DbEntityValidationException();

        if (characterGroup.HasActiveClaims())
        {
            throw new DbEntityValidationException();
        }

        _ = characterGroup.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
        _ = characterGroup.EnsureProjectActive();

        foreach (var character in characterGroup.Characters.Where(ch => ch.IsActive))
        {
            if (character.ParentCharacterGroupIds.Except([characterGroupId]).Any())
            {
                continue;
            }

            character.ParentCharacterGroupIds = character.ParentCharacterGroupIds
                .Union(characterGroup.ParentCharacterGroupIds).ToArray();
        }

        foreach (var character in characterGroup.ChildGroups.Where(ch => ch.IsActive))
        {
            if (character.ParentCharacterGroupIds.Except([characterGroupId]).Any())
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

        _ = SmartDelete(characterGroup);

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task EditProject(EditProjectRequest request)
    {
        var project = await ProjectRepository.GetProjectAsync(request.ProjectId);

        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeProjectProperties);

        project.Details.ClaimApplyRules = new MarkdownString(request.ClaimApplyRules);
        project.Details.ProjectAnnounce = new MarkdownString(request.ProjectAnnounce);
        project.Details.EnableManyCharacters = request.MultipleCharacters;
        project.Details.PublishPlot = request.PublishPlot && !project.Active;
        project.ProjectName = Required(request.ProjectName);
        project.IsAcceptingClaims = request.IsAcceptingClaims && project.Active;

        project.Details.AutoAcceptClaims = request.AutoAcceptClaims;
        project.Details.EnableAccommodation = request.IsAccommodationEnabled;
        project.Details.DefaultTemplateCharacterId = request.DefaultTemplateCharacterId?.CharacterId;

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
                _ = project.RequestMasterAccess(CurrentUserId, a => a.CanGrantRights);
            }
        }

        _ = project.EnsureProjectActive();

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

    public async Task RemoveAccess(int projectId, int userId, int? newResponsibleMasterIdOrDefault)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        if (userId != CurrentUserId)
        {
            _ = project.RequestMasterAccess(CurrentUserId, a => a.CanGrantRights);
        }

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

        if (claims.Count != 0)
        {
            if (newResponsibleMasterIdOrDefault is int newResponsible)
            {
                _ = project.RequestMasterAccess(newResponsible);

                foreach (var claim in claims)
                {
                    claim.ResponsibleMasterUserId = newResponsible;
                }
            }
            else
            {
                throw new MasterHasResponsibleException(acl);
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

        _ = UnitOfWork.GetDbSet<ProjectAcl>().Remove(acl);
        _ = UnitOfWork.GetDbSet<UserSubscription>()
            .RemoveRange(UnitOfWork.GetDbSet<UserSubscription>()
                         .Where(x => x.UserId == userId && x.ProjectId == projectId));

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task ChangeAccess(ChangeAccessRequest changeAccessRequest)
    {
        var project = await ProjectRepository.GetProjectAsync(changeAccessRequest.ProjectId);
        _ = project.RequestMasterAccess(CurrentUserId, a => a.CanGrantRights);

        var acl = project.ProjectAcls.Single(
            a => a.ProjectId == changeAccessRequest.ProjectId && a.UserId == changeAccessRequest.UserId);
        SetRightsFromRequest(changeAccessRequest, acl);

        await UnitOfWork.SaveChangesAsync();
    }

}

