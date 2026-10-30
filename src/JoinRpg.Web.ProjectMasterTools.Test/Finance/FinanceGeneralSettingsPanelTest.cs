using Bunit;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectMasterTools.Finance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Finance;

public class FinanceGeneralSettingsPanelTest : BunitContext
{
    private readonly FakeFinanceSettingsClient client = new();

    public FinanceGeneralSettingsPanelTest()
    {
        Services.AddSingleton<IFinanceSettingsClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    [Fact]
    public void SavesEditedSettings()
    {
        var cut = RenderPanel();

        cut.FindAll("input[type=checkbox]")[1].Change(true);
        cut.Find("textarea").Change("Студентам");
        cut.Find("form").Submit();

        var saved = client.SavedGeneralSettings.ShouldNotBeNull();
        saved.ProjectId.ShouldBe(new ProjectIdentification(1));
        saved.WarnOnOverPayment.ShouldBeTrue();
        saved.PreferentialFeeEnabled.ShouldBeTrue();
        saved.PreferentialFeeConditions.ShouldBe("Студентам");
        cut.Markup.ShouldContain("Общие настройки финансов сохранены");
    }

    [Fact]
    public void ServerRefusalIsShown()
    {
        client.SaveError = "Проект в архиве";
        var cut = RenderPanel();

        cut.Find("form").Submit();

        cut.Markup.ShouldContain("Проект в архиве");
        cut.Markup.ShouldNotContain("Общие настройки финансов сохранены");
    }

    [Fact]
    public void WithoutMoneyPermissionPanelIsReadOnly()
    {
        client.HasEditAccess = false;
        var cut = RenderPanel();

        cut.FindAll("button[type=submit]").ShouldBeEmpty();
        cut.Find("fieldset").HasAttribute("disabled").ShouldBeTrue();
        cut.Markup.ShouldContain("Только просмотр");
    }

    [Fact]
    public void ArchivedProjectHidesPanel()
    {
        client.ProjectStatus = ProjectLifecycleStatus.Archived;
        var cut = RenderPanel();

        cut.Markup.Trim().ShouldBeEmpty();
    }

    private IRenderedComponent<FinanceGeneralSettingsPanel> RenderPanel()
        => Render<FinanceGeneralSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));
}
