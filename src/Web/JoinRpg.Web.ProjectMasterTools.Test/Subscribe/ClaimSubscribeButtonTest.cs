using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Web.ProjectMasterTools.Subscribe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Subscribe;

public class ClaimSubscribeButtonTest : BunitContext
{
    private static readonly ClaimIdentification ClaimId = new(new ProjectIdentification(123), 5);

    private readonly FakeGameSubscribeClient client = new();

    public ClaimSubscribeButtonTest()
    {
        Services.AddSingleton<IGameSubscribeClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    [Fact]
    public void ShouldShowServerRefusal()
    {
        client.FailWith = new ServerProblemException("Проект заблокирован, изменения запрещены");
        var cut = Render<ClaimSubscribeButton>(parameters => parameters.Add(p => p.ClaimId, ClaimId));

        cut.FindAll("button").First(b => b.TextContent.Contains("Подписаться")).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Проект заблокирован, изменения запрещены"));
    }

    [Fact]
    public void ShouldShowGenericTextWhenFailsUnexpectedly()
    {
        client.FailWith = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");
        var cut = Render<ClaimSubscribeButton>(parameters => parameters.Add(p => p.ClaimId, ClaimId));

        cut.FindAll("button").First(b => b.TextContent.Contains("Подписаться")).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось подписаться на заявку"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    private class FakeGameSubscribeClient : IGameSubscribeClient
    {
        public Exception? FailWith { get; set; }

        public Task<ClaimSubscribeViewModel> GetSubscribeForClaim(ClaimIdentification claimId)
            => Task.FromResult(new ClaimSubscribeViewModel { IsDirect = false, ParentSubscribe = SubscriptionOptions.CreateNoneSet() });

        public Task<ClaimSubscribeViewModel> SubscribeClaimToUser(ClaimIdentification claimId)
            => FailWith is null
                ? Task.FromResult(new ClaimSubscribeViewModel { IsDirect = true, ParentSubscribe = SubscriptionOptions.CreateNoneSet() })
                : Task.FromException<ClaimSubscribeViewModel>(FailWith);

        public Task<ClaimSubscribeViewModel> UnsubscribeClaimToUser(ClaimIdentification claimId) => throw new NotImplementedException();

        public Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId) => throw new NotImplementedException();

        public Task RemoveSubscription(int projectId, int userSubscriptionsId) => throw new NotImplementedException();

        public Task SaveGroupSubscription(int projectId, EditSubscribeViewModel model) => throw new NotImplementedException();
    }
}
