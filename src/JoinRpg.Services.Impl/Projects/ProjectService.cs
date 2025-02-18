using System.Data.Entity.Validation;
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

namespace JoinRpg.Services.Impl.Projects;

internal class ProjectService(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IMasterEmailService masterEmailService,
    ILogger<ProjectService> logger
    ) : DbServiceImplBase(unitOfWork, currentUserAccessor), IProjectService
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



    public async Task<CharacterGroupIdentification> AddCharacterGroup(ProjectIdentification projectId,
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> parentCharacterGroupIds,
        string description)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);

        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
        _ = project.EnsureProjectActive();

        var group = Create(new CharacterGroup()
        {
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

        return new CharacterGroupIdentification(projectId, group.CharacterGroupId);
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

    public async Task EditCharacterGroup(CharacterGroupIdentification characterGroupId,
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> parentCharacterGroupIds,
        string description)
    {
        var characterGroup =
            (await ProjectRepository.GetGroupAsync(characterGroupId))
            .RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles)
            .EnsureProjectActive();

        if (characterGroup.IsRoot || characterGroup.IsSpecial) //We shoud not edit root group or special group
        {
            throw new InvalidOperationException();
        }

        characterGroup.CharacterGroupName = Required(name);
        characterGroup.IsPublic = isPublic;
        characterGroup.ParentCharacterGroupIds =
            await ValidateCharacterGroupList(characterGroupId.ProjectId,
                Required(parentCharacterGroupIds),
                ensureNotSpecial: true);
        characterGroup.Description = new MarkdownString(description);

        MarkTreeModified(characterGroup.Project); // Can be smarted than this
        MarkChanged(characterGroup);
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteCharacterGroup(int projectId, int characterGroupId)
    {
        var characterGroup = await ProjectRepository.GetGroupAsync(projectId, characterGroupId) ?? throw new DbEntityValidationException();

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
}

