using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectMasterTools.Subscribe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Subscribe;

public class MasterSubscribeListTest : BunitContext
{
    private readonly FakeGameSubscribeClient client = new();

    public MasterSubscribeListTest()
    {
        Services.AddSingleton<IGameSubscribeClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    [Fact]
    public void ShouldShowServerRefusalAndRestoreRowWhenRemoveRefused()
    {
        client.FailWith = new ServerProblemException("Проект заблокирован, изменения запрещены");
        var cut = RenderWithOneSubscription();

        cut.FindAll("button").First(b => b.TextContent.Contains("Отписаться")).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Проект заблокирован, изменения запрещены"));
        cut.Markup.ShouldContain("Гриффиндор");
    }

    [Fact]
    public void ShouldShowGenericTextAndRestoreRowWhenRemoveFailsUnexpectedly()
    {
        client.FailWith = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");
        var cut = RenderWithOneSubscription();

        cut.FindAll("button").First(b => b.TextContent.Contains("Отписаться")).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось отписаться"));
        cut.Markup.ShouldNotContain("Bad Gateway");
        cut.Markup.ShouldContain("Гриффиндор");
    }

    private IRenderedComponent<MasterSubscribeList> RenderWithOneSubscription()
    {
        var model = new SubscribeListViewModel
        {
            Items =
            [
                new SubscribeListItemViewModel
                {
                    Link = new Uri("https://joinrpg.ru/123/roles/5"),
                    Name = "Гриффиндор",
                    Options = new SubscribeOptionsViewModel(),
                    UserSubscriptionId = 7,
                    ProjectId = 123,
                },
            ],
            PaymentTypeNames = [],
            AllowChanges = true,
            ProjectId = 123,
            MasterId = 42,
        };
        return Render<MasterSubscribeList>(parameters => parameters
            .Add(p => p.ProjectId, 123)
            .Add(p => p.MasterId, 42)
            .Add(p => p.InitialModel, model));
    }

    private class FakeGameSubscribeClient : IGameSubscribeClient
    {
        public Exception? FailWith { get; set; }

        public Task RemoveSubscription(int projectId, int userSubscriptionsId)
            => FailWith is null ? Task.CompletedTask : Task.FromException(FailWith);

        public Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId) => throw new NotImplementedException();

        public Task SaveGroupSubscription(int projectId, EditSubscribeViewModel model) => throw new NotImplementedException();

        public Task<ClaimSubscribeViewModel> GetSubscribeForClaim(ClaimIdentification claimId) => throw new NotImplementedException();

        public Task<ClaimSubscribeViewModel> SubscribeClaimToUser(ClaimIdentification claimId) => throw new NotImplementedException();

        public Task<ClaimSubscribeViewModel> UnsubscribeClaimToUser(ClaimIdentification claimId) => throw new NotImplementedException();
    }
}
