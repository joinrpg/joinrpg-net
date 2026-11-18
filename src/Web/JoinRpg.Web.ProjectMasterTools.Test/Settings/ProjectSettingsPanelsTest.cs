using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Fields;
using JoinRpg.Web.ProjectMasterTools.Fields;
using JoinRpg.Web.ProjectMasterTools.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Settings;

/// <summary>
/// Группа панелей настроек игры — один остров: семь панелей в одном аккордеоне,
/// имя группы уходит панелям каскадом от JoinCollapsePanelGroup. Сбой загрузки одной
/// панели не роняет остальные (ErrorBoundary вокруг каждой).
/// </summary>
public class ProjectSettingsPanelsTest : BunitContext
{
    private readonly FakeSettingsClient settingsClient = new();

    public ProjectSettingsPanelsTest()
    {
        Services.AddSingleton<IProjectSettingsClient>(settingsClient);
        Services.AddSingleton<IProjectFieldOperationsClient>(new FakeFieldOperationsClient());
        Services.AddSingleton<IProjectFieldsClient>(new StubFieldsClient());
        Services.AddSingleton<IProjectFieldUriLocator>(new NotUsedLocator());
        _ = Services.AddJoinRpgWebComponents();
        _ = AddBunitPersistentComponentState();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("WebAssembly", isInteractive: true));
    }

    [Fact]
    public void ActiveProjectShowsAllSevenPanelsInOneAccordion()
    {
        var cut = Render<ProjectSettingsPanels>(p => p
            .Add(x => x.ProjectId, new ProjectIdentification(1))
            .Add(x => x.Active, true));

        // Все семь панелей на месте и принадлежат одному аккордеону: открыта не больше одной
        cut.WaitForAssertion(() => cut.FindAll("details.join-collapse-panel__toggle").Count.ShouldBe(7));
        cut.FindAll("details[name='project-settings']").Count.ShouldBe(7);
    }

    [Fact]
    public void ArchivedProjectShowsOnlyPublishPanel()
    {
        var cut = Render<ProjectSettingsPanels>(p => p
            .Add(x => x.ProjectId, new ProjectIdentification(1))
            .Add(x => x.Active, false));

        // В архиве остальные панели скрыты, заголовки не мелькают
        cut.WaitForAssertion(() => cut.FindAll("details.join-collapse-panel__toggle").Count.ShouldBe(1));
        cut.Find("details.join-collapse-panel__toggle").Id.ShouldBe("settings-publish");
    }

    [Fact]
    public void BrokenPanelDoesNotKillOtherPanels()
    {
        // Одна ручка ответила 500 — её панель показывает ошибку, остальные шесть работают
        settingsClient.FailTimeZoneLoad = true;

        var cut = Render<ProjectSettingsPanels>(p => p
            .Add(x => x.ProjectId, new ProjectIdentification(1))
            .Add(x => x.Active, true));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось загрузить панель"));
        cut.FindAll("details.join-collapse-panel__toggle").Count.ShouldBe(6);
        cut.Markup.ShouldContain("Основное");
    }

    private sealed class FakeSettingsClient : IProjectSettingsClient
    {
        public bool FailTimeZoneLoad { get; set; }

        public Task<ProjectMainSettingsViewModel> GetMainSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectMainSettingsViewModel
            {
                ProjectId = projectId,
                ProjectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
                HasEditAccess = true,
                ProjectName = "Тестовый проект",
                ProjectAnnounce = "",
                EnableAccommodation = false,
                PlayerInvitesEnabled = false,
            });

        public Task<ProjectClaimSettingsViewModel> GetClaimSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectClaimSettingsViewModel
            {
                ProjectId = projectId,
                ProjectName = new ProjectName("Тест"),
                ProjectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
                HasEditAccess = true,
                IsAcceptingClaims = false,
                IsPublicProject = false,
                DefaultTemplateCharacterId = null,
                AutoAcceptClaims = false,
                StrictlyOneCharacter = false,
                ClaimApplyRules = "",
            });

        public Task<ProjectCheckInSettingsViewModel> GetCheckInSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectCheckInSettingsViewModel
            {
                ProjectId = projectId,
                ProjectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
                HasEditAccess = true,
                InProgress = false,
                AllowSecondRoles = false,
                ShowCheckInBlockersToPlayers = false,
            });

        public Task<ProjectContactsSettingsViewModel> GetContactSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectContactsSettingsViewModel
            {
                ProjectId = projectId,
                ProjectName = new ProjectName("Тест"),
                ProjectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
                HasEditAccess = true,
                Telegram = MandatoryContactsView.Optional,
                Phone = MandatoryContactsView.Optional,
                Vkontakte = MandatoryContactsView.Optional,
                Fio = MandatoryContactsView.Optional,
                Passport = MandatorySenstiveContactsView.Optional,
                RegistrationAddress = MandatorySenstiveContactsView.Optional,
                BirthDate = MandatorySenstiveContactsView.Optional,
            });

        public Task<ProjectTimeZoneSettingsViewModel> GetTimeZoneSettings(ProjectIdentification projectId)
            => FailTimeZoneLoad
                ? throw new HttpRequestException("500")
                : Task.FromResult(new ProjectTimeZoneSettingsViewModel
                {
                    ProjectId = projectId,
                    ProjectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
                    HasEditAccess = true,
                    TimeZoneId = "Europe/Moscow",
                });

        public Task<ProjectPublishSettingsViewModel> GetPublishSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectPublishSettingsViewModel
            {
                ProjectId = projectId,
                ProjectName = new ProjectName("Тест"),
                ProjectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
                HasEditAccess = true,
                PublishEnabled = false,
                CloneSettings = ProjectCloneSettingsView.CloneDisabled,
            });

        public Task SaveMainSettings(ProjectMainSettingsViewModel model) => Task.CompletedTask;
        public Task SaveClaimSettings(ProjectClaimSettingsViewModel model) => Task.CompletedTask;
        public Task SaveCheckInSettings(ProjectCheckInSettingsViewModel model) => Task.CompletedTask;
        public Task SaveContactSettings(ProjectContactsSettingsViewModel model) => Task.CompletedTask;
        public Task SaveTimeZoneSettings(ProjectTimeZoneSettingsViewModel model) => Task.CompletedTask;
        public Task SavePublishSettings(ProjectPublishSettingsViewModel model) => Task.CompletedTask;
    }

    private sealed class FakeFieldOperationsClient : IProjectFieldOperationsClient
    {
        public Task<ProjectFieldSettingsViewModel> GetFieldSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectFieldSettingsViewModel
            {
                ProjectId = projectId,
                HasEditAccess = true,
                NameField = null,
                DescriptionField = null,
                NameFieldCandidates = [],
            });

        public Task Delete(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task DeleteVariant(ProjectFieldVariantIdentification variantId) => throw new NotSupportedException();
        public Task DeleteUnusedVariants(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task CreateTimeSlots(TimeSlotMassAddRequest request) => throw new NotSupportedException();
        public Task SortTimeSlotsByStartTime(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task SortVariantsByLabel(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task CreateVariants(FieldValuesMassAddRequest request) => throw new NotSupportedException();
        public Task SaveFieldSettings(ProjectFieldSettingsViewModel model) => throw new NotSupportedException();
    }

    private sealed class StubFieldsClient : IProjectFieldsClient
    {
        public Task<List<ProjectFieldDto>> GetProjectFields(ProjectIdentification projectId)
            => Task.FromResult<List<ProjectFieldDto>>([]);
    }

    private sealed class NotUsedLocator : IProjectFieldUriLocator
    {
        public Uri GetEditUri(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Uri GetCreateVariantUri(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Uri GetEditVariantUri(ProjectFieldVariantIdentification variantId) => throw new NotSupportedException();
        public Uri GetFieldSettingsUri(ProjectIdentification projectId) => throw new NotSupportedException();
    }
}
