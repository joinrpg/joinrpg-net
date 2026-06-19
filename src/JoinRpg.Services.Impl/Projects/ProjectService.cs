using System.Data.Entity.Validation;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal class ProjectService(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    MasterEmailService masterEmailService,
    ILogger<ProjectService> logger,
    IProjectMetadataRepository projectMetadataRepository,
    IProjectPropsService projectPropsService
    ) : DbServiceImplBase(unitOfWork, currentUserAccessor), IProjectService
{
    public async Task<Project> AddProject(ProjectName projectName, string rootCharacterGroupName, ProjectIdentification? cloneFrom)
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
            Details = new DataModel.ProjectDetails() { ClonedFromProjectId = cloneFrom?.Value, },
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

        _ = project.RequestMasterAccess(CurrentUserId, Permission.CanEditRoles);
        _ = project.EnsureProjectActive();

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var group = Create(new CharacterGroup()
        {
            CharacterGroupName = Required(name),
            ParentCharacterGroupIds =
                ValidateCharacterGroupList(projectInfo,
                    Required(parentCharacterGroupIds)),
            ProjectId = projectId,
            IsRoot = false,
            IsSpecial = false,
            IsPublic = isPublic,
            IsActive = true,
            Description = new MarkdownDbValue(description),
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
        _ = parentCharacterGroup.RequestMasterAccess(currentUserId, Permission.CanEditRoles);
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
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            publishPlot,
            (project, _, publish) =>
            {
                project.Active = false;
                project.IsAcceptingClaims = false;
                project.Details.PublishPlot = publish;
            });

        // Нотификация — ответственность вызывающего, после успешного сохранения (см. adr009).
        await masterEmailService.EmailProjectClosed(new ProjectClosedMail()
        {
            ProjectId = projectId,
            Initiator = new UserIdentification(CurrentUserId),
        });
    }

    public async Task CloseProjectAsStale(ProjectIdentification projectId, DateOnly lastActiveDate)
    {
        // Вызывается из фоновой джобы под роботом-админом — проверка прав проходит по admin-bypass.
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            lastActiveDate,
            (project, _, _) =>
            {
                project.Active = false;
                project.IsAcceptingClaims = false;
                project.Details.PublishPlot = false;
            });

        await masterEmailService.EmailProjectClosedStale(new ProjectClosedStaleMail()
        {
            ProjectId = projectId,
            LastActiveDate = lastActiveDate,
        });

        logger.LogInformation("Project {project} is closed as stale project.", projectId);
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

    public async Task EditCharacterGroup(CharacterGroupIdentification characterGroupId,
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> parentCharacterGroupIds,
        string description)
    {
        var characterGroup =
            (await ProjectRepository.GetGroupAsync(characterGroupId))
            .RequestMasterAccess(CurrentUserId, Permission.CanEditRoles)
            .EnsureProjectActive();

        if (characterGroup.IsRoot || characterGroup.IsSpecial) //We shoud not edit root group or special group
        {
            throw new InvalidOperationException();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(characterGroupId.ProjectId);

        var currentGroupInfo = projectInfo.Groups[characterGroupId];
        var forbiddenParents = currentGroupInfo.AllChildGroupsIncludingThis;
        var cycleViolation = Required(parentCharacterGroupIds).FirstOrDefault(p => forbiddenParents.Contains(p));
        if (cycleViolation is not null)
        {
            throw new ArgumentException($"Группа {cycleViolation.CharacterGroupId} является потомком редактируемой группы и не может быть её родителем.");
        }

        characterGroup.CharacterGroupName = Required(name);
        characterGroup.IsPublic = isPublic;
        characterGroup.ParentCharacterGroupIds =
            ValidateCharacterGroupList(projectInfo,
                Required(parentCharacterGroupIds),
                ensureNotSpecial: true);
        characterGroup.Description = new MarkdownDbValue(description);

        MarkTreeModified(characterGroup.Project); // Can be smarted than this
        MarkChanged(characterGroup);
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteCharacterGroup(int projectId, int characterGroupId)
    {
        var characterGroup = await ProjectRepository.GetGroupAsync(projectId, characterGroupId) ?? throw new DbEntityValidationException();

        _ = characterGroup.RequestMasterAccess(CurrentUserId, Permission.CanEditRoles);
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
            characterGroup.DirectlyRelatedPlotElements.CleanLinksList();
        }

        _ = SmartDelete(characterGroup);

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task EditProject(EditProjectRequest request)
    {
        await projectPropsService.ChangeProjectProperties(request.ProjectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            request,
            (project, _, req) =>
            {
                project.Details.ClaimApplyRules = new MarkdownDbValue(req.ClaimApplyRules);
                project.Details.ProjectAnnounce = new MarkdownDbValue(req.ProjectAnnounce);
                project.ProjectName = Required(req.ProjectName);
            });
    }

    public async Task SetAccommodationSettings(ProjectIdentification projectId, bool enableAccommodation)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            enableAccommodation,
            (project, _, enabled) => project.Details.EnableAccommodation = enabled);
    }

    async Task IProjectService.SetPublishSettings(ProjectIdentification projectId, ProjectCloneSettings cloneSettings, bool publishEnabled)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.AllowInactive,
            (cloneSettings, publishEnabled),
            (project, _, args) =>
            {
                project.Details.PublishPlot = args.publishEnabled && !project.Active;
                project.Details.ProjectCloneSettings = args.cloneSettings;
            });
    }

    public async Task SetCheckInSettings(ProjectIdentification projectId,
       bool checkInProgress,
       bool enableCheckInModule,
       bool modelAllowSecondRoles)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            (checkInProgress, enableCheckInModule, modelAllowSecondRoles),
            (project, _, args) =>
            {
                project.Details.CheckInProgress = args.checkInProgress && args.enableCheckInModule;
                project.Details.EnableCheckInModule = args.enableCheckInModule;
                project.Details.AllowSecondRoles = args.modelAllowSecondRoles && args.enableCheckInModule;
            });
    }

    public async Task SetContactSettings(ProjectIdentification projectId, ProjectProfileRequirementSettings settings)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            settings,
            (project, _, s) =>
            {
                project.Details.RequireRealName = s.RequireRealName;
                project.Details.RequirePhone = s.RequirePhone;
                project.Details.RequireVkontakte = s.RequireVkontakte;
                project.Details.RequireTelegram = s.RequireTelegram;
                project.Details.RequirePassport = s.RequirePassport;
                project.Details.RequireRegistrationAddress = s.RequireRegistrationAddress;
            });
    }

    public async Task SetClaimSettings(ProjectIdentification projectId, ProjectClaimSettings settings)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            settings,
            (project, _, s) =>
            {
                project.Details.EnableManyCharacters = !s.StrictlyOneCharacter;
                project.Details.IsPublicProject = s.IsPublicProject;
                project.IsAcceptingClaims = s.IsAcceptingClaims && project.Active;
                project.Details.AutoAcceptClaims = s.AutoAcceptClaims;
                project.Details.DefaultTemplateCharacterId = s.DefaultTemplate?.CharacterId;
            });
    }
}

