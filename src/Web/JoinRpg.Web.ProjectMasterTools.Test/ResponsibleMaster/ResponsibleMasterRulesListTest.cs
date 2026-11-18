using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.ResponsibleMaster;

public class ResponsibleMasterRulesListTest : BunitContext
{
    private readonly FakeResponsibleMasterRuleClient client = new();

    public ResponsibleMasterRulesListTest()
    {
        Services.AddSingleton<IResponsibleMasterRuleClient>(client);
        Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkLocator());
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    [Fact]
    public void ShouldShowServerRefusalAndRestoreRowWhenRemoveRefused()
    {
        client.FailWith = new ServerProblemException("Проект в архиве, изменения запрещены");
        var cut = RenderWithOneRule();

        cut.FindAll("button").First(b => b.TextContent.Contains("Удалить правило")).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Проект в архиве, изменения запрещены"));
        cut.FindAll("div.responsible-master-rule-list-row").Count.ShouldBe(1);
    }

    [Fact]
    public void ShouldShowGenericTextAndRestoreRowWhenRemoveFailsUnexpectedly()
    {
        client.FailWith = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");
        var cut = RenderWithOneRule();

        cut.FindAll("button").First(b => b.TextContent.Contains("Удалить правило")).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось удалить правило"));
        cut.Markup.ShouldNotContain("Bad Gateway");
        cut.FindAll("div.responsible-master-rule-list-row").Count.ShouldBe(1);
    }

    private IRenderedComponent<ResponsibleMasterRulesList> RenderWithOneRule()
    {
        var model = new ResponsibleMasterRuleListViewModel(
            [new ResponsibleMasterRuleViewModel(Id: 5, "Гриффиндор", UserLinkViewModel.Hidden)],
            HasEditAccess: true);
        return Render<ResponsibleMasterRulesList>(parameters => parameters
            .Add(p => p.ProjectId, 123)
            .Add(p => p.InitialModel, model));
    }

    private class FakeResponsibleMasterRuleClient : IResponsibleMasterRuleClient
    {
        public Exception? FailWith { get; set; }

        public Task RemoveResponsibleMasterRule(ProjectIdentification projectId, int ruleId)
            => FailWith is null ? Task.CompletedTask : Task.FromException(FailWith);

        public Task<ResponsibleMasterRuleListViewModel> AddResponsibleMasterRule(ProjectIdentification projectId, int groupId, int masterId)
            => throw new NotImplementedException();

        public Task<ResponsibleMasterRuleListViewModel> GetResponsibleMasterRuleList(ProjectIdentification projectId)
            => throw new NotImplementedException();
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new("https://joinrpg.ru/user/" + target.UserId?.Value);
    }
}
