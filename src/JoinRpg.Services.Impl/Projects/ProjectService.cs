using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal class ProjectService(
    ICurrentUserAccessor currentUserAccessor,
    MasterEmailService masterEmailService,
    ILogger<ProjectService> logger,
    IProjectPropsService projectPropsService
    ) : IProjectService
{
    /// <param name="ownerRole">Роль создателя проекта на странице мастеров (ADR019).</param>
    public Task<Project> AddProject(ProjectName projectName, string rootCharacterGroupName, ProjectIdentification? cloneFrom, string ownerRole)
        => projectPropsService.CreateProject(
            (projectName, rootCharacterGroupName, cloneFrom, ownerRole),
            ctx =>
            {
                var rootGroup = new CharacterGroup()
                {
                    IsPublic = true,
                    IsRoot = true,
                    CharacterGroupName = ctx.Request.rootCharacterGroupName,
                    IsActive = true,
                    ResponsibleMasterUserId = ctx.CurrentUser.UserId,
                };
                ctx.MarkCreatedNow(rootGroup);

                var project = new Project()
                {
                    Active = true,
                    IsAcceptingClaims = false,
                    CreatedDate = ctx.Now.UtcDateTime,
                    ProjectName = ctx.Request.projectName,
                    CharacterGroups = [rootGroup,],
                    ProjectAcls = [ProjectAcl.CreateRootAcl(ctx.CurrentUser.UserId, ctx.Request.ownerRole, isOwner: true),],
                    Details = new DataModel.ProjectDetails() { ClonedFromProjectId = ctx.Request.cloneFrom?.Value, },
                    ProjectFields = [],
                };

                return project;
            });

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
            Initiator = new UserIdentification(currentUserAccessor.UserId),
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
                ctx.Project.ProjectName = ServiceValidation.Required(ctx.Request.ProjectName);
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
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.AllowArchived,
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

    public async Task SetTimeZone(ProjectIdentification projectId, TimeZoneInfo timeZone)
    {
        // В БД храним только IANA-идентификаторы; Windows-идентификатор (например, TimeZoneInfo.Local на Windows) не пропускаем.
        if (!timeZone.HasIanaId)
        {
            throw new ArgumentException($"Часовой пояс {timeZone.Id} не имеет IANA-идентификатора", nameof(timeZone)); // TODO[Localize]
        }
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            timeZone.Id,
            ctx => ctx.Project.Details.TimeZoneId = ctx.Request);
    }
}

