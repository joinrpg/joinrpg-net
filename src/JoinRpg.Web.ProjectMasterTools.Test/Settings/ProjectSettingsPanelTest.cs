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

    private class FakeSettingsClient : IProjectSettingsClient
    {
        public bool FailNextSave { get; set; }
        public int SavedCount { get; private set; }

        public Task<ProjectPublishSettingsViewModel> GetPublishSettings(ProjectIdentification projectId)
            => Task.FromResult(new ProjectPublishSettingsViewModel
            {
                ProjectId = projectId,
                ProjectName = new ProjectName("Тест"),
                ProjectStatus = ProjectLifecycleStatus.ActiveClaimsOpen,
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

        public Task SaveContactSettings(ProjectContactsSettingsViewModel model) => throw new NotImplementedException();
        public Task<ProjectContactsSettingsViewModel> GetContactSettings(ProjectIdentification projectId) => throw new NotImplementedException();
        public Task SaveClaimSettings(ProjectClaimSettingsViewModel model) => throw new NotImplementedException();
        public Task<ProjectClaimSettingsViewModel> GetClaimSettings(ProjectIdentification projectId) => throw new NotImplementedException();
        public Task SaveTimeZoneSettings(ProjectTimeZoneSettingsViewModel model) => throw new NotImplementedException();
        public Task<ProjectTimeZoneSettingsViewModel> GetTimeZoneSettings(ProjectIdentification projectId) => throw new NotImplementedException();
    }
}
