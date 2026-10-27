using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Projects;

/// <summary>
/// Тесты методов <see cref="ProjectService"/>, переведённых на <see cref="IProjectPropsService"/>.
/// Собирается реальный <see cref="ProjectService"/> поверх реального <c>ProjectPropsService</c> и
/// фейков репозиториев — результат проверяется через пересобранный <see cref="ProjectInfo"/>.
/// </summary>
public class ProjectServiceTest : ProjectMetadataServiceTestBase
{
    private readonly FakeNotificationService notifications = new();
    private static readonly UserInfoHeader siteAdmin = new(new UserIdentification(100500), new UserDisplayName("Админ", null));
    private IReadOnlyCollection<UserInfoHeader> admins = [siteAdmin];

    // Возвращаем интерфейс намеренно: SetPublishSettings — явная реализация IProjectService.
#pragma warning disable CA1859
    private IProjectService CreateService(int? currentUserId = null, bool isAdmin = false)
#pragma warning restore CA1859
    {
        var currentUser = CreateCurrentUser(currentUserId, isAdmin);
        var masterEmailService = new MasterEmailService(notifications, metadataRepository, new FakeVirtualUsersService(),
            FakeUserRepository.WithAdmins([.. admins]));

        return new ProjectService(
            currentUser,
            masterEmailService,
            NullLogger<ProjectService>.Instance,
            CreatePropsService(currentUser));
    }

    [Fact]
    public async Task EditProject_ChangesNameAndAnnounce()
    {
        await CreateService().EditProject(new EditProjectRequest
        {
            ProjectId = ProjectId,
            ProjectName = "Новое название",
            ProjectAnnounce = "Анонс",
        });

        Result.ProjectName.Value.ShouldBe("Новое название");
        mock.Project.Details.ProjectAnnounce.Contents.ShouldBe("Анонс");
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task SetAccommodationSettings_EnablesAccommodation()
    {
        await CreateService().SetAccommodationSettings(ProjectId, enableAccommodation: true);

        Result.AccommodationSettings.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task SetTimeZone_ChangesTimeZone()
    {
        await CreateService().SetTimeZone(ProjectId, TimeZoneInfo.FindSystemTimeZoneById("Asia/Yekaterinburg"));

        Result.TimeZone.Id.ShouldBe("Asia/Yekaterinburg");
        mock.Project.Details.TimeZoneId.ShouldBe("Asia/Yekaterinburg");
    }

    [Fact]
    public async Task SetTimeZone_WithoutIanaId_Throws()
    {
        var withoutIanaId = TimeZoneInfo.CreateCustomTimeZone("Custom", TimeSpan.FromHours(3), "Custom", "Custom");

        await Should.ThrowAsync<ArgumentException>(() => CreateService().SetTimeZone(ProjectId, withoutIanaId));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task SetPublishSettings_UpdatesCloneSettings()
    {
        await CreateService().SetPublishSettings(ProjectId, ProjectCloneSettings.CanBeClonedByAnyone, publishEnabled: true);

        Result.CloneSettings.ShouldBe(ProjectCloneSettings.CanBeClonedByAnyone);
        // PublishPlot включается только для неактивного проекта; мок активен.
        Result.PublishPlot.ShouldBeFalse();
    }

    [Fact]
    public async Task SetCheckInSettings_EnablesProgressAndSecondRoles()
    {
        await CreateService().SetCheckInSettings(ProjectId, new ProjectCheckInSettings(InProgress: true, AllowSecondRoles: true, ShowCheckInBlockersToPlayers: false));

        Result.ProjectCheckInSettings.InProgress.ShouldBeTrue();
        Result.ProjectCheckInSettings.AllowSecondRoles.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task SetCheckInSettings_ChangesShowCheckInBlockersToPlayers(bool before, bool after)
    {
        mock.Project.Details.EnableCheckInModule = before;

        await CreateService().SetCheckInSettings(ProjectId, mock.ProjectInfo.ProjectCheckInSettings with { ShowCheckInBlockersToPlayers = after });

        Result.ProjectCheckInSettings.ShowCheckInBlockersToPlayers.ShouldBe(after);
        mock.Project.Details.EnableCheckInModule.ShouldBe(after);
    }

    [Fact]
    public async Task SetContactSettings_AppliesRequirements()
    {
        var settings = new ProjectProfileRequirementSettings(
            RequireRealName: MandatoryStatus.Required,
            RequireTelegram: MandatoryStatus.Optional,
            RequireVkontakte: MandatoryStatus.Optional,
            RequirePhone: MandatoryStatus.Required,
            RequirePassport: MandatoryStatus.Optional,
            RequireRegistrationAddress: MandatoryStatus.Optional,
            RequireBirthDate: MandatoryStatus.Recommended);

        await CreateService().SetContactSettings(ProjectId, settings);

        Result.ProfileRequirementSettings.RequireRealName.ShouldBe(MandatoryStatus.Required);
        Result.ProfileRequirementSettings.RequirePhone.ShouldBe(MandatoryStatus.Required);
        Result.ProfileRequirementSettings.RequireBirthDate.ShouldBe(MandatoryStatus.Recommended);
    }

    // #5706: согласие давалось на прежний набор защищённых данных — на новый его спрашивают заново.
    [Fact]
    public async Task SetContactSettings_NewSensitiveDataRequested_RevokesConsents()
    {
        mock.Project.Details.RequirePassport = MandatoryStatus.Recommended;
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.PlayerAllowedSenstiveData = true;
        mock.ReInitProjectInfo();

        await CreateService().SetContactSettings(ProjectId,
            mock.ProjectInfo.ProfileRequirementSettings with { RequireBirthDate = MandatoryStatus.Recommended });

        claim.PlayerAllowedSenstiveData.ShouldBeFalse();
        Result.ProfileRequirementSettings.BirthDateRequested.ShouldBeTrue();
    }

    [Theory]
    [InlineData(MandatoryStatus.Recommended, MandatoryStatus.Recommended)] // ничего нового
    [InlineData(MandatoryStatus.Recommended, MandatoryStatus.Optional)] // запрос снят
    public async Task SetContactSettings_NothingNewRequested_KeepsConsents(MandatoryStatus before, MandatoryStatus after)
    {
        mock.Project.Details.RequirePassport = before;
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.PlayerAllowedSenstiveData = true;
        mock.ReInitProjectInfo();

        await CreateService().SetContactSettings(ProjectId,
            mock.ProjectInfo.ProfileRequirementSettings with { RequirePassport = after, RequirePhone = MandatoryStatus.Required });

        claim.PlayerAllowedSenstiveData.ShouldBeTrue();
    }

    // Настройки контактов разрешены и на время восстановления (ADR023) — отзыв тоже.
    [Fact]
    public async Task SetContactSettings_BlockedProject_RevokesConsents()
    {
        mock.Project.IsBlocked = true;
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.PlayerAllowedSenstiveData = true;
        mock.ReInitProjectInfo();

        await CreateService().SetContactSettings(ProjectId,
            mock.ProjectInfo.ProfileRequirementSettings with { RequirePassport = MandatoryStatus.Recommended });

        claim.PlayerAllowedSenstiveData.ShouldBeFalse();
    }

    [Fact]
    public async Task SetContactSettings_ArchivedProject_ThrowsAndKeepsConsents()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.PlayerAllowedSenstiveData = true;

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(() => CreateService()
            .SetContactSettings(ProjectId, mock.ProjectInfo.ProfileRequirementSettings with { RequirePassport = MandatoryStatus.Recommended }));

        claim.PlayerAllowedSenstiveData.ShouldBeTrue();
    }

    [Fact]
    public async Task SetContactSettings_WithoutPermission_DoesNotRevokeConsents()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.PlayerAllowedSenstiveData = true;

        _ = await Should.ThrowAsync<NoAccessToProjectException>(() => CreateService(currentUserId: mock.Player.UserId)
            .SetContactSettings(ProjectId, mock.ProjectInfo.ProfileRequirementSettings with { RequirePassport = MandatoryStatus.Recommended }));

        claim.PlayerAllowedSenstiveData.ShouldBeTrue();
    }

    [Fact]
    public async Task SetClaimSettings_AppliesSettings()
    {
        var settings = new ProjectClaimSettings(
            DefaultTemplate: null,
            StrictlyOneCharacter: true,
            AutoAcceptClaims: true,
            IsAcceptingClaims: true,
            IsPublicProject: true);

        await CreateService().SetClaimSettings(ProjectId, settings);

        Result.ClaimSettings.StrictlyOneCharacter.ShouldBeTrue();
        Result.ClaimSettings.AutoAcceptClaims.ShouldBeTrue();
        Result.ClaimSettings.IsPublicProject.ShouldBeTrue();
        // IsAcceptingClaims разрешается только для активного проекта; мок активен.
        Result.ClaimSettings.IsAcceptingClaims.ShouldBeTrue();
    }

    // Флаг «что мешает регистрации» живёт в настройках регистрации, настройки заявок его не трогают.
    [Fact]
    public async Task SetClaimSettings_KeepsShowCheckInBlockersToPlayers()
    {
        mock.Project.Details.EnableCheckInModule = true;

        await CreateService().SetClaimSettings(ProjectId, mock.ProjectInfo.ClaimSettings);

        mock.Project.Details.EnableCheckInModule.ShouldBeTrue();
    }

    [Fact]
    public async Task SetClaimSettings_WithRules_ChangesRulesInSameSave()
    {
        await CreateService().SetClaimSettings(ProjectId, mock.ProjectInfo.ClaimSettings, claimApplyRules: "Правила подачи");

        mock.Project.Details.ClaimApplyRules.Contents.ShouldBe("Правила подачи");
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task SetClaimSettings_WithoutRules_KeepsRules()
    {
        mock.Project.Details.ClaimApplyRules = new MarkdownDbValue("Старые правила");

        await CreateService().SetClaimSettings(ProjectId, mock.ProjectInfo.ClaimSettings with { StrictlyOneCharacter = true });

        mock.Project.Details.ClaimApplyRules.Contents.ShouldBe("Старые правила");
    }

    [Fact]
    public async Task CloseProject_DeactivatesProjectAndNotifiesMasters()
    {
        await CreateService().CloseProject(ProjectId, publishPlot: true);

        Result.IsActive.ShouldBeFalse();
        mock.Project.Active.ShouldBeFalse();
        mock.Project.IsAcceptingClaims.ShouldBeFalse();
        mock.Project.Details.PublishPlot.ShouldBeTrue();
        // Нотификация мастерам поставлена в очередь вызывающим (ProjectService), а не сервисом метаданных.
        notifications.Queued.ShouldHaveSingleItem();
    }

    // ADR023: закрытие заблокированного проекта — конец восстановления, флаг блокировки сбрасывается.
    [Fact]
    public async Task CloseProject_Blocked_ArchivesAndClearsBlock()
    {
        mock.Project.IsBlocked = true;

        await CreateService().CloseProject(ProjectId, publishPlot: false);

        Result.ProjectStatus.ShouldBe(ProjectLifecycleStatus.Archived);
        mock.Project.IsBlocked.ShouldBeFalse();
        notifications.Queued.ShouldHaveSingleItem();
    }

    // Блокировки не было — снимать нечего, и уведомлять не о чем.
    [Fact]
    public async Task UnblockProject_ActiveProject_DoesNothing()
    {
        await CreateService().UnblockProject(ProjectId);

        Result.ProjectStatus.ShouldBe(ProjectLifecycleStatus.ActiveClaimsOpen);
        notifications.Queued.ShouldBeEmpty();
    }

    // ADR023: после снятия блокировки проект — как был, включая открытый приём заявок.
    [Fact]
    public async Task UnblockProject_RestoresPreviousStatus()
    {
        mock.Project.IsBlocked = true;

        await CreateService().UnblockProject(ProjectId);

        mock.Project.IsBlocked.ShouldBeFalse();
        Result.ProjectStatus.ShouldBe(ProjectLifecycleStatus.ActiveClaimsOpen);
    }

    // Снятие блокировки необратимо (ADR023) — узнают все мастера проекта и админы сайта, со ссылкой на проект.
    [Fact]
    public async Task UnblockProject_Blocked_NotifiesMastersAndAdmins()
    {
        mock.Project.IsBlocked = true;
        var masterIds = mock.ProjectInfo.Masters.Select(m => m.UserId).ToList();

        await CreateService().UnblockProject(ProjectId);

        var notification = notifications.Queued.ShouldHaveSingleItem();
        notification.EntityReference.ShouldBe(ProjectId);
        notification.Initiator.ShouldBe(new UserIdentification(mock.Master.UserId));
        notification.TemplateText.TemplateContents.ShouldContain("Test снял(а) блокировку");
        notification.Recepients.Select(r => r.UserId).ShouldBe([.. masterIds, siteAdmin.UserId], ignoreOrder: true);
    }

    [Fact]
    public async Task UnblockProject_AdminIsMaster_NotifiedOnce()
    {
        mock.Project.IsBlocked = true;
        var masterAdmin = new UserInfoHeader(new UserIdentification(mock.Master.UserId), new UserDisplayName("Мастер-админ", null));
        admins = [masterAdmin, siteAdmin];

        await CreateService().UnblockProject(ProjectId);

        var recipients = notifications.Queued.ShouldHaveSingleItem().Recepients;
        recipients.Select(r => r.UserId).ShouldBeUnique();
        recipients.ShouldContain(r => r.UserId == masterAdmin.UserId);
        recipients.ShouldContain(r => r.UserId == siteAdmin.UserId);
    }

    [Fact]
    public async Task UnblockProject_Archived_Throws()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;

        await Should.ThrowAsync<ProjectDeactivatedException>(() => CreateService().UnblockProject(ProjectId));
    }

    [Fact]
    public async Task CloseProjectAsStale_Blocked_Throws()
    {
        mock.Project.IsBlocked = true;
        var service = CreateService(currentUserId: mock.Player.UserId, isAdmin: true);

        await Should.ThrowAsync<ProjectBlockedException>(() => service.CloseProjectAsStale(ProjectId, new DateOnly(2020, 1, 1)));
        mock.Project.Active.ShouldBeTrue();
    }

    [Fact]
    public async Task CloseProjectAsStale_DeactivatesAndNotifies()
    {
        // Джоба выполняется под роботом-админом: проверка прав проходит по admin-bypass,
        // несмотря на отсутствие мастерского доступа у пользователя.
        var service = CreateService(currentUserId: mock.Player.UserId, isAdmin: true);

        await service.CloseProjectAsStale(ProjectId, new DateOnly(2020, 1, 1));

        Result.IsActive.ShouldBeFalse();
        mock.Project.Active.ShouldBeFalse();
        mock.Project.IsAcceptingClaims.ShouldBeFalse();
        notifications.Queued.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SettingsChange_WithoutAccess_Throws()
    {
        var service = CreateService(currentUserId: mock.Player.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => service.SetAccommodationSettings(ProjectId, enableAccommodation: true));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    // ADR023: настройки со страницы настроек проекта доступны и на время восстановления.
    [Fact]
    public async Task EditProject_Blocked_Allowed()
    {
        mock.Project.IsBlocked = true;

        await CreateService().EditProject(new EditProjectRequest
        {
            ProjectId = ProjectId,
            ProjectName = "Новое название",
            ProjectAnnounce = "",
        });

        Result.ProjectName.Value.ShouldBe("Новое название");
    }

    [Fact]
    public async Task SetAccommodationSettings_Blocked_Allowed()
    {
        mock.Project.IsBlocked = true;

        await CreateService().SetAccommodationSettings(ProjectId, enableAccommodation: true);

        Result.AccommodationSettings.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task SetContactSettings_Blocked_Allowed()
    {
        mock.Project.IsBlocked = true;

        await CreateService().SetContactSettings(ProjectId,
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePhone = MandatoryStatus.Required });

        Result.ProfileRequirementSettings.RequirePhone.ShouldBe(MandatoryStatus.Required);
    }

    [Fact]
    public async Task SetClaimSettings_Blocked_Allowed()
    {
        mock.Project.IsBlocked = true;

        await CreateService().SetClaimSettings(ProjectId, new ProjectClaimSettings(
            DefaultTemplate: null,
            StrictlyOneCharacter: true,
            AutoAcceptClaims: false,
            IsAcceptingClaims: true,
            IsPublicProject: false));

        Result.ClaimSettings.StrictlyOneCharacter.ShouldBeTrue();
        Result.ProjectStatus.ShouldBe(ProjectLifecycleStatus.Blocked);
    }

    [Fact]
    public async Task SetTimeZone_Blocked_Allowed()
    {
        mock.Project.IsBlocked = true;

        await CreateService().SetTimeZone(ProjectId, TimeZoneInfo.FindSystemTimeZoneById("Asia/Yekaterinburg"));

        Result.TimeZone.Id.ShouldBe("Asia/Yekaterinburg");
    }

    [Fact]
    public async Task SetPublishSettings_Blocked_Allowed()
    {
        mock.Project.IsBlocked = true;

        await CreateService().SetPublishSettings(ProjectId, ProjectCloneSettings.CanBeClonedByAnyone, publishEnabled: true);

        Result.CloneSettings.ShouldBe(ProjectCloneSettings.CanBeClonedByAnyone);
        Result.PublishPlot.ShouldBeFalse();
    }

    // Регистрация настраивается на своей странице и в блокировке закрыта.
    [Fact]
    public async Task SetCheckInSettings_Blocked_Throws()
    {
        mock.Project.IsBlocked = true;

        await Should.ThrowAsync<ProjectBlockedException>(() => CreateService().SetCheckInSettings(ProjectId, new ProjectCheckInSettings(InProgress: true, AllowSecondRoles: true, ShowCheckInBlockersToPlayers: true)));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }
}
