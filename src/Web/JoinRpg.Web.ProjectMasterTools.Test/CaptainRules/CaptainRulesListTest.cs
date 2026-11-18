using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.ProjectMasterTools.CaptainRules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.CaptainRules;

public class CaptainRulesListTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(123);

    private readonly FakeCaptainRuleClient client = new();

    public CaptainRulesListTest()
    {
        Services.AddSingleton<ICaptainRuleClient>(client);
        Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkLocator());
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    [Fact]
    public void ShouldShowServerRefusalAndRestoreRowWhenRemoveRefused()
    {
        client.FailWith = new ServerProblemException("Проект заблокирован, изменения запрещены");
        var cut = RenderWithOneRule();

        cut.FindAll("button").First(b => b.TextContent.Contains("Удалить доступ капитана")).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Проект заблокирован, изменения запрещены"));
        cut.FindAll("div.rule-list-row").Count.ShouldBe(1);
    }

    [Fact]
    public void ShouldShowGenericTextAndRestoreRowWhenRemoveFailsUnexpectedly()
    {
        client.FailWith = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");
        var cut = RenderWithOneRule();

        cut.FindAll("button").First(b => b.TextContent.Contains("Удалить доступ капитана")).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось удалить правило"));
        cut.Markup.ShouldNotContain("Bad Gateway");
        cut.FindAll("div.rule-list-row").Count.ShouldBe(1);
    }

    private IRenderedComponent<CaptainRulesList> RenderWithOneRule()
    {
        var rule = new CaptainAccessRule(new CharacterGroupIdentification(ProjectId, 5), new UserIdentification(42), CanApprove: false);
        var model = new CaptainRuleListViewModel(
            [new CaptainRuleViewModel(rule, "Гриффиндор", UserLinkViewModel.Hidden)],
            HasEditAccess: true);
        return Render<CaptainRulesList>(parameters => parameters
            .Add(p => p.ProjectId, ProjectId)
            .Add(p => p.InitialModel, model));
    }

    private class FakeCaptainRuleClient : ICaptainRuleClient
    {
        public Exception? FailWith { get; set; }

        public Task Remove(CaptainAccessRule rule) => FailWith is null ? Task.CompletedTask : Task.FromException(FailWith);

        public Task<CaptainRuleListViewModel> AddOrChange(CaptainAccessRule rule) => throw new NotImplementedException();

        public Task<CaptainRuleListViewModel> GetList(ProjectIdentification projectId) => throw new NotImplementedException();
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new("https://joinrpg.ru/user/" + target.UserId?.Value);
    }
}
