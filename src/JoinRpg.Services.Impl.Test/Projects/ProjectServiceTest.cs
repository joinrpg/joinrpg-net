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

    // Возвращаем интерфейс намеренно: SetPublishSettings — явная реализация IProjectService.
#pragma warning disable CA1859
    private IProjectService CreateService(int? currentUserId = null, bool isAdmin = false)
#pragma warning restore CA1859
    {
        var currentUser = CreateCurrentUser(currentUserId, isAdmin);
        var masterEmailService = new MasterEmailService(notifications, metadataRepository, new FakeVirtualUsersService());

        return new ProjectService(
            currentUser,
            masterEmailService,
            NullLogger<ProjectService>.Instance,
            CreatePropsService(currentUser));
    }

    [Fact]
    public async Task EditProject_ChangesNameAndTexts()
    {
        await CreateService().EditProject(new EditProjectRequest
        {
            ProjectId = ProjectId,
            ProjectName = "Новое название",
            ClaimApplyRules = "Правила подачи",
            ProjectAnnounce = "Анонс",
        });

        Result.ProjectName.Value.ShouldBe("Новое название");
        mock.Project.Details.ClaimApplyRules.Contents.ShouldBe("Правила подачи");
        mock.Project.Details.ProjectAnnounce.Contents.ShouldBe("Анонс");
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task SetAccommodationSettings_EnablesAccommodation()
    {
        await CreateService().SetAccommodationSettings(ProjectId, enableAccommodation: true);

        Result.AccomodationEnabled.ShouldBeTrue();
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
    public async Task SetCheckInSettings_EnablesModuleAndProgress()
    {
        await CreateService().SetCheckInSettings(ProjectId,
            checkInProgress: true,
            enableCheckInModule: true,
            modelAllowSecondRoles: true);

        Result.ProjectCheckInSettings.CheckInModuleEnabled.ShouldBeTrue();
        Result.ProjectCheckInSettings.InProgress.ShouldBeTrue();
        Result.ProjectCheckInSettings.AllowSecondRoles.ShouldBeTrue();
    }

    [Fact]
    public async Task SetCheckInSettings_WithoutModule_DisablesDependentFlags()
    {
        await CreateService().SetCheckInSettings(ProjectId,
            checkInProgress: true,
            enableCheckInModule: false,
            modelAllowSecondRoles: true);

        Result.ProjectCheckInSettings.CheckInModuleEnabled.ShouldBeFalse();
        Result.ProjectCheckInSettings.InProgress.ShouldBeFalse();
        Result.ProjectCheckInSettings.AllowSecondRoles.ShouldBeFalse();
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
            RequireRegistrationAddress: MandatoryStatus.Optional);

        await CreateService().SetContactSettings(ProjectId, settings);

        Result.ProfileRequirementSettings.RequireRealName.ShouldBe(MandatoryStatus.Required);
        Result.ProfileRequirementSettings.RequirePhone.ShouldBe(MandatoryStatus.Required);
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
}
