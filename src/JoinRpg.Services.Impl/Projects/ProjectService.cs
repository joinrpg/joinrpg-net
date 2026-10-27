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
        => AddProject(projectName, rootCharacterGroupName, cloneFrom, ownerRole, isBlocked: false);

    // TODO[Localize]
    public async Task<ProjectIdentification> AddRestoredProjectShell(ProjectName projectName)
    {
        var project = await AddProject(
            projectName,
            rootCharacterGroupName: "Все роли",
            cloneFrom: null,
            ownerRole: CreateProjectService.GetOwnerRole(ProjectTypeDto.Larp),
            isBlocked: true);
        return new ProjectIdentification(project.ProjectId);
    }

    private Task<Project> AddProject(ProjectName projectName, string rootCharacterGroupName, ProjectIdentification? cloneFrom, string ownerRole, bool isBlocked)
        => projectPropsService.CreateProject(
            (projectName, rootCharacterGroupName, cloneFrom, ownerRole, isBlocked),
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
                    IsBlocked = ctx.Request.isBlocked,
                    CreatedDate = ctx.Now.UtcDateTime,
                    ProjectName = ctx.Request.projectName,
                    CharacterGroups = [rootGroup,],
                    ProjectAcls = [ProjectAcl.CreateRootAcl(ctx.CurrentUser.UserId, ctx.Request.ownerRole, isOwner: true),],
                    // Незаполненные поля заявки новый проект игрокам показывает; клон затем копирует флаг оригинала.
                    Details = new DataModel.ProjectDetails() { ClonedFromProjectId = ctx.Request.cloneFrom?.Value, EnableCheckInModule = true, },
                    ProjectFields = [],
                };

                return project;
            });

    public async Task CloseProject(ProjectIdentification projectId, bool publishPlot)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.AllowBlocked,
            publishPlot,
            ctx =>
            {
                ctx.Project.Active = false;
                ctx.Project.IsAcceptingClaims = false;
                ctx.Project.IsBlocked = false;
                ctx.Project.Details.PublishPlot = ctx.Request;
            });

        // Нотификация — ответственность вызывающего, после успешного сохранения (см. adr009).
        await masterEmailService.EmailProjectClosed(new ProjectClosedMail()
        {
            ProjectId = projectId,
            Initiator = new UserIdentification(currentUserAccessor.UserId),
        });
    }

    public async Task UnblockProject(ProjectIdentification projectId)
    {
        var wasBlocked = false;
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.AllowBlocked,
            projectId,
            ctx =>
            {
                wasBlocked = ctx.Project.IsBlocked;
                ctx.Project.IsBlocked = false;
            });

        // Блокировки не было — снимать нечего, и сообщать не о чем.
        if (!wasBlocked)
        {
            return;
        }

        // Нотификация — ответственность вызывающего, после успешного сохранения (см. adr009).
        await masterEmailService.EmailProjectUnblocked(new ProjectUnblockedMail()
        {
            ProjectId = projectId,
            Initiator = currentUserAccessor.ToUserInfoHeader(),
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
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.AllowBlocked, // настройки проекта разрешены и на время восстановления (ADR023)
            request,
            ctx =>
            {
                ctx.Project.Details.ProjectAnnounce = new MarkdownDbValue(ctx.Request.ProjectAnnounce);
                ctx.Project.ProjectName = ctx.Request.ProjectName.Value;
            });
    }

    public async Task SetAccommodationSettings(ProjectIdentification projectId, bool enableAccommodation)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.AllowBlocked, // настройки проекта разрешены и на время восстановления (ADR023)
            enableAccommodation,
            ctx => ctx.Project.Details.EnableAccommodation = ctx.Request);
    }

    async Task IProjectService.SetPublishSettings(ProjectIdentification projectId, ProjectCloneSettings cloneSettings, bool publishEnabled)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.AllowArchivedOrBlocked, // настройки проекта разрешены и на время восстановления (ADR023)
            (cloneSettings, publishEnabled),
            ctx =>
            {
                ctx.Project.Details.PublishPlot = ctx.Request.publishEnabled && !ctx.Project.Active;
                ctx.Project.Details.ProjectCloneSettings = ctx.Request.cloneSettings;
            });
    }

    public async Task SetCheckInSettings(ProjectIdentification projectId, ProjectCheckInSettings settings)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
            settings,
            ctx =>
            {
                ctx.Project.Details.CheckInProgress = ctx.Request.InProgress;
                ctx.Project.Details.AllowSecondRoles = ctx.Request.AllowSecondRoles;
            });
    }

    public async Task SetContactSettings(ProjectIdentification projectId, ProjectProfileRequirementSettings settings)
    {
        await projectPropsService.ChangeProjectPropertiesAsync(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.AllowBlocked, // настройки проекта разрешены и на время восстановления (ADR023)
            settings,
            async ctx =>
            {
                // Согласие игрок давал на прежний набор защищённых данных — на новый его надо спросить заново.
                if (ctx.Request.RequestsNewSensitiveDataComparedTo(ctx.ProjectInfo.ProfileRequirementSettings))
                {
                    var revoked = await ctx.RevokeSensitiveDataConsents();
                    if (revoked > 0) // у нового проекта (клонирование) заявок нет — шуметь в лог незачем
                    {
                        logger.LogInformation(
                            "Проект {projectId} запросил новые защищённые данные, отозвано согласий: {revokedCount}",
                            projectId,
                            revoked);
                    }
                }

                ctx.Project.Details.RequireRealName = ctx.Request.RequireRealName;
                ctx.Project.Details.RequirePhone = ctx.Request.RequirePhone;
                ctx.Project.Details.RequireVkontakte = ctx.Request.RequireVkontakte;
                ctx.Project.Details.RequireTelegram = ctx.Request.RequireTelegram;
                ctx.Project.Details.RequirePassport = ctx.Request.RequirePassport;
                ctx.Project.Details.RequireRegistrationAddress = ctx.Request.RequireRegistrationAddress;
                ctx.Project.Details.RequireBirthDate = ctx.Request.RequireBirthDate;
            });
    }

    public async Task SetClaimSettings(ProjectIdentification projectId, ProjectClaimSettings settings, string? claimApplyRules = null)
    {
        await projectPropsService.ChangeProjectProperties(projectId,
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.AllowBlocked, // настройки проекта разрешены и на время восстановления (ADR023)
            (settings, ClaimApplyRulesChanged: claimApplyRules is not null), // аргументы уходят в лог — без текста правил
            ctx =>
            {
                ctx.Project.Details.EnableManyCharacters = !ctx.Request.settings.StrictlyOneCharacter;
                ctx.Project.Details.IsPublicProject = ctx.Request.settings.IsPublicProject;
                ctx.Project.IsAcceptingClaims = ctx.Request.settings.IsAcceptingClaims && ctx.Project.Active;
                ctx.Project.Details.AutoAcceptClaims = ctx.Request.settings.AutoAcceptClaims;
                ctx.Project.Details.DefaultTemplateCharacterId = ctx.Request.settings.DefaultTemplate?.CharacterId;
                ctx.Project.Details.EnableCheckInModule = ctx.Request.settings.ShowCheckInBlockersToPlayers;
                if (claimApplyRules is not null)
                {
                    ctx.Project.Details.ClaimApplyRules = new MarkdownDbValue(claimApplyRules);
                }
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
            Permission.CanChangeProjectProperties, ProjectActiveRequirement.AllowBlocked, // настройки проекта разрешены и на время восстановления (ADR023)
            timeZone.Id,
            ctx => ctx.Project.Details.TimeZoneId = ctx.Request);
    }
}

