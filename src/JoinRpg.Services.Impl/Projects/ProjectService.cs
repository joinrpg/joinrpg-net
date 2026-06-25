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
            ctx =>
            {
                ctx.Project.Active = false;
                ctx.Project.IsAcceptingClaims = false;
                ctx.Project.Details.PublishPlot = ctx.Request;
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
            ctx =>
            {
                ctx.Project.Active = false;
                ctx.Project.IsAcceptingClaims = false;
                ctx.Project.Details.PublishPlot = false;
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
            ctx =>
            {
                ctx.Project.Details.ClaimApplyRules = new MarkdownDbValue(ctx.Request.ClaimApplyRules);
                ctx.Project.Details.ProjectAnnounce = new MarkdownDbValue(ctx.Request.ProjectAnnounce);
                ctx.Project.ProjectName = Required(ctx.Request.ProjectName);
            });
    }

    public async Task SetAccommodationSettings(ProjectIdentification projectId, bool enableAccommodation)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            enableAccommodation,
            ctx => ctx.Project.Details.EnableAccommodation = ctx.Request);
    }

    async Task IProjectService.SetPublishSettings(ProjectIdentification projectId, ProjectCloneSettings cloneSettings, bool publishEnabled)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.AllowInactive,
            (cloneSettings, publishEnabled),
            ctx =>
            {
                ctx.Project.Details.PublishPlot = ctx.Request.publishEnabled && !ctx.Project.Active;
                ctx.Project.Details.ProjectCloneSettings = ctx.Request.cloneSettings;
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
            ctx =>
            {
                ctx.Project.Details.CheckInProgress = ctx.Request.checkInProgress && ctx.Request.enableCheckInModule;
                ctx.Project.Details.EnableCheckInModule = ctx.Request.enableCheckInModule;
                ctx.Project.Details.AllowSecondRoles = ctx.Request.modelAllowSecondRoles && ctx.Request.enableCheckInModule;
            });
    }

    public async Task SetContactSettings(ProjectIdentification projectId, ProjectProfileRequirementSettings settings)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            settings,
            ctx =>
            {
                ctx.Project.Details.RequireRealName = ctx.Request.RequireRealName;
                ctx.Project.Details.RequirePhone = ctx.Request.RequirePhone;
                ctx.Project.Details.RequireVkontakte = ctx.Request.RequireVkontakte;
                ctx.Project.Details.RequireTelegram = ctx.Request.RequireTelegram;
                ctx.Project.Details.RequirePassport = ctx.Request.RequirePassport;
                ctx.Project.Details.RequireRegistrationAddress = ctx.Request.RequireRegistrationAddress;
            });
    }

    public async Task SetClaimSettings(ProjectIdentification projectId, ProjectClaimSettings settings)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            settings,
            ctx =>
            {
                ctx.Project.Details.EnableManyCharacters = !ctx.Request.StrictlyOneCharacter;
                ctx.Project.Details.IsPublicProject = ctx.Request.IsPublicProject;
                ctx.Project.IsAcceptingClaims = ctx.Request.IsAcceptingClaims && ctx.Project.Active;
                ctx.Project.Details.AutoAcceptClaims = ctx.Request.AutoAcceptClaims;
                ctx.Project.Details.DefaultTemplateCharacterId = ctx.Request.DefaultTemplate?.CharacterId;
            });
    }
}

