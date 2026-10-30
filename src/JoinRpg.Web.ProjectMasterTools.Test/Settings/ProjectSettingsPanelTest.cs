using Bunit;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectMasterTools.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Settings;

public class ProjectSettingsPanelTest : BunitContext
{
    private const string ServerError = "Неизвестная серверная ошибка при сохранении";
    private readonly FakeSettingsClient client = new();

    public ProjectSettingsPanelTest()
    {
        Services.AddSingleton<IProjectSettingsClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    [Fact]
    public void FieldChangeAfterServerErrorEnablesSaveAgain()
    {
        client.FailNextSave = true;
        var cut = Render<ProjectPublishSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));

        cut.Find("form").Submit();

        cut.Markup.ShouldContain(ServerError);
        cut.Find("button[type=submit]").HasAttribute("disabled").ShouldBeFalse();

        cut.Find("input[type=radio][value=CanBeClonedByAnyone]").Change("CanBeClonedByAnyone");

        cut.Markup.ShouldNotContain(ServerError);
        cut.Find("button[type=submit]").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void ResubmitAfterServerErrorSaves()
    {
        client.FailNextSave = true;
        var cut = Render<ProjectPublishSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));

        cut.Find("form").Submit();
        cut.Find("form").Submit();

        cut.Markup.ShouldNotContain(ServerError);
        cut.Markup.ShouldContain("Настройки публикации сохранены");
        client.SavedCount.ShouldBe(1);
    }

    private const string NewSensitiveDataWarning = "Вы запрашиваете новые защищенные данные";

    // #5706: при сохранении сервер отзовёт выданные согласия — мастера предупреждаем, пока форма не сохранена.
    [Fact]
    public void RequestingNewSensitiveDataShowsWarningUntilSaved()
    {
        var cut = Render<ProjectContactRequirementsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));
        cut.Markup.ShouldNotContain(NewSensitiveDataWarning);

        // Паспорт уже запрошен — его повторный выбор ничего нового не просит.
        cut.FindAll("input[type=radio][value=Request]")[0].Change("Request");
        cut.Markup.ShouldNotContain(NewSensitiveDataWarning);

        // Третья группа «Запросить» — дата рождения, которой в сохранённых настройках нет.
        cut.FindAll("input[type=radio][value=Request]")[2].Change("Request");
        cut.Markup.ShouldContain(NewSensitiveDataWarning);

        cut.Find("form").Submit();
        cut.Markup.ShouldNotContain(NewSensitiveDataWarning);
    }

    [Fact]
    public void SensitiveDataWarningStaysAfterFailedSaveAndGoesAwayWhenRequestWithdrawn()
    {
        var cut = Render<ProjectContactRequirementsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));
        cut.FindAll("input[type=radio][value=Request]")[2].Change("Request");

        client.FailNextSave = true;
        cut.Find("form").Submit();
        cut.Markup.ShouldContain(ServerError);
        cut.Markup.ShouldContain(NewSensitiveDataWarning);

        // Третья группа «Не запрашивать» — снова как в сохранённых настройках.
        cut.FindAll("input[type=radio][value=Optional]")[^1].Change("Optional");
        cut.Markup.ShouldNotContain(NewSensitiveDataWarning);
    }

    private const string ClaimApplyRules = "Заявки только от совершеннолетних";

    // Правила подачи видны только при включённом приёме заявок, но скрытые не должны стираться при сохранении.
    [Fact]
    public void HiddenClaimApplyRulesAreSavedUnchanged()
    {
        var cut = Render<ProjectClaimSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));
        cut.FindAll("textarea").ShouldBeEmpty();

        cut.Find("form").Submit();

        client.SavedClaimSettings.ShouldNotBeNull().ClaimApplyRules.ShouldBe(ClaimApplyRules);
    }

    // #952: «Разрешить игрокам приглашать соседей» имеет смысл только при включённом поселении.
    // Скрытая настройка не должна стираться при сохранении остальных полей панели.
    [Fact]
    public void HiddenPlayerInvitesEnabledIsSavedUnchanged()
    {
        var cut = Render<ProjectMainSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));
        cut.FindAll("input[type=checkbox][name=PlayerInvitesEnabled]").ShouldBeEmpty();

        cut.Find("form").Submit();

        client.SavedMainSettings.ShouldNotBeNull().PlayerInvitesEnabled.ShouldBeTrue();
    }

    [Fact]
    public void PlayerInvitesEnabledIsSaved()
    {
        client.EnableAccommodation = true;
        var cut = Render<ProjectMainSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));
        cut.Find("input[type=checkbox][name=PlayerInvitesEnabled]").Change(false);

        cut.Find("form").Submit();

        client.SavedMainSettings.ShouldNotBeNull().PlayerInvitesEnabled.ShouldBeFalse();
    }

    // Название проекта короче 5 букв сервер не примет — сохранить его нельзя уже в форме.
    // Пробелы по краям сервер обрежет, поэтому и в форме они в длину не идут (#5855).
    [Theory]
    [InlineData("Тест")]
    [InlineData("   Тест   ")]
    public void TooShortProjectNameIsNotSaved(string projectName)
    {
        var cut = Render<ProjectMainSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));

        cut.Find("input[type=text]").Input(projectName);
        cut.Find("button[type=submit]").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("form").Submit();

        client.SavedMainSettings.ShouldBeNull();
    }

    // Ошибку поля показывает FormRowFor под полем; сводка над формой — только для ошибок всей модели (серверных).
    [Fact]
    public void FieldErrorIsShownOnce()
    {
        var cut = Render<ProjectMainSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));

        cut.Find("input[type=text]").Input("Тест");

        cut.FindAll(".validation-message, .validation-errors li")
            .Count(e => e.TextContent.Contains("Название проекта должно быть длиной"))
            .ShouldBe(1);
    }

    [Fact]
    public void ShowCheckInBlockersToPlayersIsSavedWithCheckInSettings()
    {
        var cut = Render<ProjectCheckInSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));

        cut.Find("input[type=checkbox][name=ShowCheckInBlockersToPlayers]").Change(true);
        cut.Find("form").Submit();

        client.SavedCheckInSettings.ShouldNotBeNull().ShowCheckInBlockersToPlayers.ShouldBeTrue();
    }

    // Настройки регистрации переехали со страницы /checkin/setup панелью на страницу настроек проекта.
    [Fact]
    public void CheckInSettingsAreSaved()
    {
        var cut = Render<ProjectCheckInSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));

        cut.Find("input[type=checkbox][name=InProgress]").Change(true);
        cut.Find("form").Submit();

        var saved = client.SavedCheckInSettings.ShouldNotBeNull();
        saved.InProgress.ShouldBeTrue();
        saved.AllowSecondRoles.ShouldBeTrue();
        cut.Markup.ShouldContain("Настройки регистрации сохранены");
    }

    // Страницу настроек видит любой мастер: без права панель показывает настройки, но изменить их не даёт.
    [Fact]
    public void WithoutEditAccessPanelIsReadOnly()
    {
        client.HasEditAccess = false;
        var cut = Render<ProjectMainSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));

        cut.FindAll("button[type=submit]").ShouldBeEmpty();
        cut.Find("fieldset").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("input[type=text]").GetAttribute("value").ShouldBe("Тестовый проект");
        cut.Markup.ShouldContain("Только просмотр");
    }

    private class FakeSettingsClient : IProjectSettingsClient
    {
        public bool HasEditAccess { get; set; } = true;

        public bool EnableAccommodation { get; set; } = false;

        public ProjectMainSettingsViewModel? SavedMainSettings { get; private set; }

        public Task<ProjectMainSettingsViewModel> GetMainSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectMainSettingsViewModel
            {
                ProjectId = projectId,
                ProjectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
                HasEditAccess = HasEditAccess,
                ProjectName = "Тестовый проект",
                ProjectAnnounce = "",
                EnableAccommodation = EnableAccommodation,
                PlayerInvitesEnabled = true,
            });

        public Task SaveMainSettings(ProjectMainSettingsViewModel model)
        {
            SavedMainSettings = model;
            SavedCount++;
            return Task.CompletedTask;
        }

        public bool FailNextSave { get; set; }
        public int SavedCount { get; private set; }

        public Task<ProjectPublishSettingsViewModel> GetPublishSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectPublishSettingsViewModel
            {
                ProjectId = projectId,
                ProjectName = new ProjectName("Тест"),
                ProjectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
                HasEditAccess = HasEditAccess,
                PublishEnabled = false,
                CloneSettings = ProjectCloneSettingsView.CloneDisabled,
            });

        public Task SavePublishSettings(ProjectPublishSettingsViewModel model)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new InvalidOperationException("Сбой сети");
            }
            SavedCount++;
            return Task.CompletedTask;
        }

        public Task SaveContactSettings(ProjectContactsSettingsViewModel model)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new InvalidOperationException("Сбой сети");
            }
            SavedCount++;
            return Task.CompletedTask;
        }

        public Task<ProjectContactsSettingsViewModel> GetContactSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectContactsSettingsViewModel
            {
                ProjectId = projectId,
                ProjectName = new ProjectName("Тест"),
                ProjectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
                HasEditAccess = HasEditAccess,
                Telegram = MandatoryContactsView.Optional,
                Phone = MandatoryContactsView.Optional,
                Vkontakte = MandatoryContactsView.Optional,
                Fio = MandatoryContactsView.Optional,
                Passport = MandatorySenstiveContactsView.Request,
                RegistrationAddress = MandatorySenstiveContactsView.Optional,
                BirthDate = MandatorySenstiveContactsView.Optional,
            });
        public ProjectClaimSettingsViewModel? SavedClaimSettings { get; private set; }

        public Task SaveClaimSettings(ProjectClaimSettingsViewModel model)
        {
            SavedClaimSettings = model;
            SavedCount++;
            return Task.CompletedTask;
        }

        // Приём заявок выключен: так панель не рисует выбор шаблона, которому нужен JS-модуль.
        public Task<ProjectClaimSettingsViewModel> GetClaimSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectClaimSettingsViewModel
            {
                ProjectId = projectId,
                ProjectName = new ProjectName("Тест"),
                ProjectStatus = ProjectLifecycleStatus.ActiveClaimsClosed,
                HasEditAccess = HasEditAccess,
                IsAcceptingClaims = false,
                IsPublicProject = false,
                DefaultTemplateCharacterId = null,
                AutoAcceptClaims = false,
                StrictlyOneCharacter = false,
                ClaimApplyRules = ClaimApplyRules,
            });
        public Task SaveTimeZoneSettings(ProjectTimeZoneSettingsViewModel model) => throw new NotImplementedException();

        public ProjectCheckInSettingsViewModel? SavedCheckInSettings { get; private set; }

        public Task SaveCheckInSettings(ProjectCheckInSettingsViewModel model)
        {
            SavedCheckInSettings = model;
            SavedCount++;
            return Task.CompletedTask;
        }

        // Вторые роли уже разрешены — проверяем, что непотроганный флаг уходит на сервер как был.
        public Task<ProjectCheckInSettingsViewModel> GetCheckInSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectCheckInSettingsViewModel
            {
                ProjectId = projectId,
                ProjectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
                HasEditAccess = HasEditAccess,
                InProgress = false,
                AllowSecondRoles = true,
                ShowCheckInBlockersToPlayers = false,
            });
        public Task<ProjectTimeZoneSettingsViewModel> GetTimeZoneSettings(ProjectIdentification projectId) => throw new NotImplementedException();
    }
}
