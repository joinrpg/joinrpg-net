using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.Portal.Controllers;
using JoinRpg.WebPortal.Managers.Comments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Переходы <c>goto/*</c> по владельцу обсуждения (ADR026 §1, #4986): несуществующий
/// комментарий/финоперация/обсуждение и обсуждение чужого проекта — 404 (#5782), чужому — отказ,
/// остальным — переход на заявку или тему.
/// </summary>
public class DiscussionRedirectControllerTest
{
    private static readonly CommentDiscussionId ClaimDiscussionId = new(700);
    private static readonly CommentDiscussionId ThreadDiscussionId = new(701);
    private static readonly CommentId ClaimCommentId = new(7001);
    private static readonly CommentId ThreadCommentId = new(7011);
    private const int ThreadId = 50;

    private readonly MockedProject mock = new();
    private readonly FakeCommentDiscussionRepository discussions = new();
    private readonly DataModel.Claim claim;

    public DiscussionRedirectControllerTest()
    {
        claim = mock.CreateClaim(mock.Character, mock.Player);
        var projectId = mock.ProjectInfo.ProjectId;
        _ = discussions
            .Add(new CommentDiscussionInfo(ClaimDiscussionId, new ClaimDiscussionOwner(claim.GetId()), [Comment(ClaimCommentId)]))
            .Add(new CommentDiscussionInfo(
                ThreadDiscussionId,
                new ForumDiscussionOwner(
                    new ForumThreadIdentification(projectId, ThreadId),
                    new CharacterGroupIdentification(projectId, mock.Group.CharacterGroupId),
                    IsVisibleToPlayer: true),
                [Comment(ThreadCommentId)]));
    }

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private static CommentInfo Comment(CommentId id)
        => new(
            id,
            ParentCommentId: null,
            new UserIdentification(2),
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            IsCommentByPlayer: false,
            IsVisibleToPlayer: true,
            ExtraAction: null,
            new MarkdownString("текст"),
            Finance: null);

    private DiscussionRedirectController CreateController(int userId)
    {
        var currentUser = new FakeCurrentUserAccessor(userId);
        var loader = new CommentDiscussionOwnerLoader(
            discussions,
            new FakeProjectMetadataRepository(mock),
            new FakeClaimInfoRepository(mock),
            new FakeCharacterInfoRepository(mock),
            currentUser);
        return new DiscussionRedirectController(loader, currentUser) { Url = new RecordingUrlHelper() };
    }

    private DiscussionRedirectController CreateMasterController() => CreateController(mock.Master.UserId);

    [Fact]
    public async Task ToFinanceOperation_MissingOperation_ReturnsNotFound()
    {
        var result = await CreateMasterController().ToFinanceOperation(ProjectId, financeOperationId: 637699);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ToComment_MissingComment_ReturnsNotFound()
    {
        var result = await CreateMasterController().ToComment(ProjectId, commentid: 637699);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ToDiscussion_MissingDiscussion_ReturnsNotFound()
    {
        var result = await CreateMasterController().ToDiscussion(ProjectId, commentDiscussionId: 637699);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ToDiscussion_DiscussionOfOtherProject_ReturnsNotFound()
    {
        var otherProject = new ProjectIdentification(ProjectId.Value + 1);

        (await CreateMasterController().ToDiscussion(otherProject, ClaimDiscussionId.Value)).ShouldBeOfType<NotFoundResult>();
        (await CreateMasterController().ToComment(otherProject, ClaimCommentId.Value)).ShouldBeOfType<NotFoundResult>();
        (await CreateMasterController().ToFinanceOperation(otherProject, ClaimCommentId.Value)).ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ToDiscussion_Claim_RedirectsToClaim()
    {
        var result = await CreateMasterController().ToDiscussion(ProjectId, ClaimDiscussionId.Value);

        result.ShouldBeOfType<RedirectResult>().Url.ShouldBe($"Claim/Edit?ClaimId={claim.ClaimId}&ProjectId={ProjectId.Value}");
    }

    [Fact]
    public async Task ToComment_Thread_RedirectsToThreadWithAnchor()
    {
        var result = await CreateMasterController().ToComment(ProjectId, ThreadCommentId.Value);

        result.ShouldBeOfType<RedirectResult>().Url
            .ShouldBe($"Forum/ViewThread?ProjectId={ProjectId.Value}&ForumThreadId={ThreadId}#comment{ThreadCommentId.Value}");
    }

    [Fact]
    public async Task ToFinanceOperation_PlayerOfClaim_RedirectsToClaimWithAnchor()
    {
        var result = await CreateController(mock.Player.UserId).ToFinanceOperation(ProjectId, ClaimCommentId.Value);

        result.ShouldBeOfType<RedirectResult>().Url
            .ShouldBe($"Claim/Edit?ClaimId={claim.ClaimId}&ProjectId={ProjectId.Value}#comment{ClaimCommentId.Value}");
    }

    [Fact]
    public async Task ToDiscussion_Stranger_NoAccess()
    {
        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateController(userId: 999).ToDiscussion(ProjectId, ClaimDiscussionId.Value));
    }

    [Fact]
    public async Task ToDiscussion_PlayerOutsideThreadGroup_NoAccess()
    {
        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateController(mock.Player.UserId).ToDiscussion(ProjectId, ThreadDiscussionId.Value));
    }

    /// <summary>Строит адрес из контроллера, действия и значений маршрута — чтобы проверить, куда ведёт переход.</summary>
    private sealed class RecordingUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext => throw new NotSupportedException();

        public string? Action(UrlActionContext actionContext)
        {
            var values = new Microsoft.AspNetCore.Routing.RouteValueDictionary(actionContext.Values);
            return $"{actionContext.Controller}/{actionContext.Action}?" + string.Join("&", values.Select(v => $"{v.Key}={v.Value}"));
        }

        public string? Content(string? contentPath) => throw new NotSupportedException();

        public bool IsLocalUrl(string? url) => throw new NotSupportedException();

        public string? Link(string? routeName, object? values) => throw new NotSupportedException();

        public string? RouteUrl(UrlRouteContext routeContext) => throw new NotSupportedException();
    }
}
