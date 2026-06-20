using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal class ProjectService(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    MasterEmailService masterEmailService,
    ILogger<ProjectService> logger,
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

