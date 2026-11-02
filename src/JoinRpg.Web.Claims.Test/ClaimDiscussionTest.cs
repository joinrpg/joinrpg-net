using Bunit.TestDoubles;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.Web.Claims.Comments;
using JoinRpg.Web.Claims.Finance;
using JoinRpg.Web.Comments;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Обсуждение заявки (ADR026, шаг 8): финблок в комментарии с финоперацией и остров модерации — только
/// для операции, которую текущий пользователь вправе подтвердить.
/// </summary>
public class ClaimDiscussionTest : BunitContext
{
    private static readonly ClaimIdentification ClaimId = new(new ProjectIdentification(123), 45);

    public ClaimDiscussionTest()
    {
        _ = Services
            .AddSingleton<ICommentUriLocator, FakeCommentUriLocator>()
            .AddSingleton<IUriLocator<UserLinkViewModel>, FakeUserLinkLocator>()
            .AddSingleton<IUriLocator<ClaimIdentification>, FakeClaimUriLocator>()
            .AddSingleton<AntiforgeryStateProvider, FakeAntiforgeryStateProvider>();
        // Сам остров проверяет FinanceModerationButtonTest; здесь важно, есть ли он и с какой операцией.
        ComponentFactories.AddStub<FinanceModerationButton>();
    }

    private IRenderedComponent<ClaimDiscussion> RenderDiscussion(params CommentViewModel[] comments)
        => Render<ClaimDiscussion>(parameters => parameters
            .Add(c => c.Model, new DiscussionViewModel(ClaimId.ProjectId, new CommentDiscussionId(9), HasMasterAccess: true, comments))
            .Add(c => c.ClaimId, ClaimId));

    private static string OwnRowText(IRenderedComponent<ClaimDiscussion> cut, int commentId)
        => cut.Find($"#comment{commentId} > .join-comment__row").TextContent;

    [Fact]
    public void Payment_ShowsAmountDateAndPaymentType()
    {
        var cut = RenderDiscussion(Comment(1, Finance(isProposed: false)));

        var text = OwnRowText(cut, 1);
        text.ShouldContain("Оплата денег игроком");
        text.ShouldContain("500");
        text.ShouldContain("25.10.2026");
        text.ShouldContain("Способ оплаты");
        text.ShouldContain("Наличными — Мастер");
        text.ShouldNotContain("Ждет одобрения мастером");
        cut.FindComponents<Stub<FinanceModerationButton>>().ShouldBeEmpty();
    }

    [Fact]
    public void OperationWithoutAmount_ShowsDateButNoAmountLine()
    {
        var cut = RenderDiscussion(Comment(1, Finance(isProposed: false) with { OperationText = null, MoneyAmount = 0 }));

        var text = OwnRowText(cut, 1);
        text.ShouldContain("Дата операции");
        text.ShouldNotContain("Оплата денег игроком");
    }

    [Fact]
    public void TransferToMaster_ShowsCounterpartyClaimLinkWithPlayer()
    {
        var cut = RenderDiscussion(Comment(1, Finance(isProposed: false) with
        {
            OperationText = "Перевод денег другому игроку",
            MoneyAmount = -500,
            Transfer = new CommentFinanceTransferViewModel(
                "Кому переведено",
                new UserLinkViewModel(new UserIdentification(8), "Игрок", ViewMode.Show),
                new ClaimLinkViewModel(ClaimId, new UserDisplayName("Игрок", "Игрок Фулл"), "Персонаж контрагента", "", new UserIdentification(8))),
        }));

        var text = OwnRowText(cut, 1);
        text.ShouldContain("Кому переведено");
        text.ShouldContain("(игрок");
        text.ShouldContain("Игрок");
        var claimLink = cut.Find("#comment1 a[href='https://test/123/claim/45/edit']");
        claimLink.TextContent.ShouldContain("Персонаж контрагента");
    }

    [Fact]
    public void TransferToPlayer_ShowsPlayerNameWithoutClaimLink()
    {
        var cut = RenderDiscussion(Comment(1, Finance(isProposed: false) with
        {
            OperationText = "Перевод денег от другого игрока",
            MoneyAmount = 500,
            Transfer = new CommentFinanceTransferViewModel(
                "От кого переведено",
                new UserLinkViewModel(new UserIdentification(8), "Игрок", ViewMode.Show),
                ClaimLink: null),
        }));

        var text = OwnRowText(cut, 1);
        text.ShouldContain("От кого переведено");
        text.ShouldContain("Игрок");
        cut.FindAll("#comment1 a[href^='https://test/123/claim/']").ShouldBeEmpty();
    }

    [Fact]
    public void CommentWithoutFinance_HasNoFinanceBlock()
    {
        var cut = RenderDiscussion(Comment(1, finance: null));

        OwnRowText(cut, 1).ShouldNotContain("Дата операции");
    }

    [Fact]
    public void ProposedOperation_ThatUserCanModerate_GetsModerationIsland()
    {
        var cut = RenderDiscussion(Comment(1, Finance(isProposed: true, canModerate: true)));

        var island = cut.FindComponent<Stub<FinanceModerationButton>>();
        var operation = island.Instance.Parameters.Get(button => button.Operation);
        operation.ShouldBe(new FinanceModerationOperationView(
            new FinanceOperationIdentification(ClaimId, 1),
            "Оплата",
            500,
            new DateOnly(2026, 10, 25),
            "Наличными — Мастер"));
        OwnRowText(cut, 1).ShouldNotContain("Ждет одобрения мастером");
    }

    [Fact]
    public void ProposedOperation_ThatUserCannotModerate_IsMarkedWaiting()
    {
        var cut = RenderDiscussion(Comment(1, Finance(isProposed: true, canModerate: false)));

        OwnRowText(cut, 1).ShouldContain("Ждет одобрения мастером");
        cut.FindComponents<Stub<FinanceModerationButton>>().ShouldBeEmpty();
    }

    [Fact]
    public void DeclinedOperation_IsMarkedDeclined()
    {
        var cut = RenderDiscussion(Comment(1, Finance(isProposed: false, isDeclined: true)));

        OwnRowText(cut, 1).ShouldContain("Мастер не подтверждает оплату");
        cut.FindComponents<Stub<FinanceModerationButton>>().ShouldBeEmpty();
    }

    [Fact]
    public void ModerationIsland_IsShownInReplyToo()
    {
        var reply = Comment(2, Finance(isProposed: true, canModerate: true), depth: 1);
        var cut = RenderDiscussion(Comment(1, finance: null, replies: [reply]));

        cut.FindComponent<Stub<FinanceModerationButton>>().Instance.Parameters.Get(button => button.Operation)
            .OperationId.ShouldBe(new FinanceOperationIdentification(ClaimId, 2));
    }

    private static CommentFinanceViewModel Finance(bool isProposed, bool canModerate = false, bool isDeclined = false)
        => new(
            "Оплата денег игроком",
            "Оплата",
            500,
            new DateOnly(2026, 10, 25),
            "Наличными — Мастер",
            isDeclined,
            isProposed,
            canModerate);

    private static CommentViewModel Comment(
        int id,
        CommentFinanceViewModel? finance,
        int depth = 0,
        CommentViewModel[]? replies = null)
        => new(
            new CommentId(id),
            new UserLinkViewModel(new UserIdentification(7), "Автор", ViewMode.Show),
            AuthorAvatarUri: null,
            new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero),
            IsCommentByPlayer: true,
            IsVisibleToPlayer: true,
            IsRead: true,
            ExtraActionText: null,
            TextHtml: $"<p>Текст {id}</p>",
            finance,
            CanConceal: false,
            depth,
            replies ?? []);

    /// <summary>Токен, как его выдал бы сервер: без него AntiforgeryToken ничего не рисует.</summary>
    private sealed class FakeAntiforgeryStateProvider : AntiforgeryStateProvider
    {
        public override AntiforgeryRequestToken? GetAntiforgeryToken() => new("test-token", "__RequestVerificationToken");
    }

    private sealed class FakeCommentUriLocator : ICommentUriLocator
    {
        public Uri GetCreateCommentUri(ProjectIdentification projectId) => new($"https://test/{projectId.Value}/forums/createcomment");

        public Uri GetConcealCommentUri(ProjectIdentification projectId) => new($"https://test/{projectId.Value}/forums/concealcomment");

        public Uri GetCommentUri(ProjectIdentification projectId, CommentId commentId) => new($"https://test/{projectId.Value}/comment/{commentId.Value}");
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://test/user/{target.UserId?.Value}");
    }

    private sealed class FakeClaimUriLocator : IUriLocator<ClaimIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new($"https://test/{target.ProjectId.Value}/claim/{target.ClaimId}/edit");
    }
}
