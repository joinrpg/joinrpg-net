using JoinRpg.DomainTypes;
using JoinRpg.Web.Comments;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

/// <summary>
/// Формы обсуждения (ADR026 §3) отправляются в те же экшены, что и старые cshtml-формы: адреса
/// серверного локатора должны совпадать с маршрутами <c>ForumController</c> и <c>DiscussionRedirectController</c>.
/// </summary>
public class CommentUriLocatorTests(IntegrationTestPortalFactory factory)
    : IClassFixture<IntegrationTestPortalFactory>
{
    private static readonly ProjectIdentification ProjectId = new(1620);

    private ICommentUriLocator Locator => factory.Services.GetRequiredService<ICommentUriLocator>();

    [Fact]
    public void CreateComment_GoesToForumCreateComment()
        => Locator.GetCreateCommentUri(ProjectId).AbsolutePath.ShouldBe("/1620/forums/createcomment", StringCompareShould.IgnoreCase);

    [Fact]
    public void ConcealComment_GoesToForumConcealComment()
        => Locator.GetConcealCommentUri(ProjectId).AbsolutePath.ShouldBe("/1620/forums/concealcomment", StringCompareShould.IgnoreCase);

    [Fact]
    public void Comment_GoesToDiscussionRedirect()
        => Locator.GetCommentUri(ProjectId, new CommentId(77)).AbsolutePath.ShouldBe("/1620/goto/comment/77", StringCompareShould.IgnoreCase);
}
