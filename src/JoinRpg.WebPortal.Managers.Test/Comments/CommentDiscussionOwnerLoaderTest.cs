using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.WebPortal.Managers.Comments;

namespace JoinRpg.WebPortal.Managers.Test.Comments;

/// <summary>
/// Владелец обсуждения и доступ к нему (ADR026 §1, #4986): мастер, игрок заявки, игрок темы,
/// чужой проект.
/// </summary>
public class CommentDiscussionOwnerLoaderTest
{
    private static readonly CommentDiscussionId ClaimDiscussionId = new(700);
    private static readonly CommentDiscussionId ThreadDiscussionId = new(701);
    private static readonly CommentId ClaimCommentId = new(7001);
    private static readonly CommentId ThreadCommentId = new(7011);

    private readonly MockedProject mock = new();
    private readonly FakeCommentDiscussionRepository discussions = new();

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

    private ClaimDiscussionOwner AddClaimDiscussion(DataModel.Claim claim)
    {
        var owner = new ClaimDiscussionOwner(claim.GetId());
        _ = discussions.Add(new CommentDiscussionInfo(ClaimDiscussionId, owner, [Comment(ClaimCommentId)]));
        return owner;
    }

    private ForumDiscussionOwner AddThreadDiscussion(bool isVisibleToPlayer = true)
    {
        var owner = new ForumDiscussionOwner(
            new ForumThreadIdentification(mock.ProjectInfo.ProjectId, 50),
            new CharacterGroupIdentification(mock.ProjectInfo.ProjectId, mock.Group.CharacterGroupId),
            isVisibleToPlayer);
        _ = discussions.Add(new CommentDiscussionInfo(ThreadDiscussionId, owner, [Comment(ThreadCommentId)]));
        return owner;
    }

    private CommentDiscussionOwnerLoader CreateLoader(UserIdentification currentUser)
        => new(
            discussions,
            new FakeProjectMetadataRepository(mock),
            new FakeClaimInfoRepository(mock),
            new FakeCharacterInfoRepository(mock),
            new FakeCurrentUserAccessor(currentUser));

    private UserIdentification Master => new(mock.Master.UserId);
    private UserIdentification Player => new(mock.Player.UserId);
    private static UserIdentification Stranger => new(999);

    [Fact]
    public async Task MissingDiscussion_ReturnsNull()
        => (await CreateLoader(Master).GetOwnerOrDefault(mock.ProjectInfo.ProjectId, new CommentDiscussionId(1))).ShouldBeNull();

    [Fact]
    public async Task MissingComment_ReturnsNull()
        => (await CreateLoader(Master).GetOwnerByCommentOrDefault(mock.ProjectInfo.ProjectId, new CommentId(1))).ShouldBeNull();

    [Fact]
    public async Task DiscussionOfOtherProject_ReturnsNull()
    {
        // id обсуждения глобально уникален: мастер проекта из маршрута не должен пройти по обсуждению чужого.
        _ = AddClaimDiscussion(mock.CreateClaim(mock.Character, mock.Player));
        var otherProject = new ProjectIdentification(mock.ProjectInfo.ProjectId.Value + 1);

        (await CreateLoader(Master).GetOwnerOrDefault(otherProject, ClaimDiscussionId)).ShouldBeNull();
        (await CreateLoader(Master).GetOwnerByCommentOrDefault(otherProject, ClaimCommentId)).ShouldBeNull();
    }

    [Fact]
    public async Task Master_HasAccessToClaimDiscussion()
    {
        var owner = AddClaimDiscussion(mock.CreateClaim(mock.Character, mock.Player));

        var access = await CreateLoader(Master).GetOwnerOrDefault(mock.ProjectInfo.ProjectId, ClaimDiscussionId);

        access.ShouldNotBeNull();
        access.Owner.ShouldBe(owner);
        access.ProjectInfo.ShouldBeSameAs(mock.ProjectInfo);
        access.HasAccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Master_HasAccessToHiddenThread()
    {
        _ = AddThreadDiscussion(isVisibleToPlayer: false);

        var access = await CreateLoader(Master).GetOwnerOrDefault(mock.ProjectInfo.ProjectId, ThreadDiscussionId);

        access.ShouldNotBeNull().HasAccess.ShouldBeTrue();
    }

    [Fact]
    public async Task PlayerOfClaim_HasAccess_ByComment()
    {
        var owner = AddClaimDiscussion(mock.CreateClaim(mock.Character, mock.Player));

        var access = await CreateLoader(Player).GetOwnerByCommentOrDefault(mock.ProjectInfo.ProjectId, ClaimCommentId);

        access.ShouldNotBeNull();
        access.Owner.ShouldBe(owner);
        access.HasAccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Stranger_NoAccessToClaimDiscussion()
    {
        _ = AddClaimDiscussion(mock.CreateClaim(mock.Character, mock.Player));

        var access = await CreateLoader(Stranger).GetOwnerOrDefault(mock.ProjectInfo.ProjectId, ClaimDiscussionId);

        access.ShouldNotBeNull().HasAccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ApprovedPlayerInGroup_HasAccessToThread()
    {
        MockedProject.AddCharToGroup(mock.Character, mock.Group);
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);
        _ = AddThreadDiscussion();

        var access = await CreateLoader(Player).GetOwnerByCommentOrDefault(mock.ProjectInfo.ProjectId, ThreadCommentId);

        access.ShouldNotBeNull().HasAccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckedInPlayerInGroup_HasAccessToThread()
    {
        MockedProject.AddCharToGroup(mock.Character, mock.Group);
        _ = mock.CreateCheckedInClaim(mock.Character, mock.Player);
        _ = AddThreadDiscussion();

        var access = await CreateLoader(Player).GetOwnerOrDefault(mock.ProjectInfo.ProjectId, ThreadDiscussionId);

        access.ShouldNotBeNull().HasAccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ApprovedPlayerInGroup_NoAccessToHiddenThread()
    {
        MockedProject.AddCharToGroup(mock.Character, mock.Group);
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);
        _ = AddThreadDiscussion(isVisibleToPlayer: false);

        var access = await CreateLoader(Player).GetOwnerOrDefault(mock.ProjectInfo.ProjectId, ThreadDiscussionId);

        access.ShouldNotBeNull().HasAccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ApprovedPlayerOutsideGroup_NoAccessToThread()
    {
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);
        _ = AddThreadDiscussion();

        var access = await CreateLoader(Player).GetOwnerOrDefault(mock.ProjectInfo.ProjectId, ThreadDiscussionId);

        access.ShouldNotBeNull().HasAccess.ShouldBeFalse();
    }

    [Fact]
    public async Task NotApprovedPlayerInGroup_NoAccessToThread()
    {
        MockedProject.AddCharToGroup(mock.Character, mock.Group);
        _ = mock.CreateClaim(mock.Character, mock.Player);
        _ = AddThreadDiscussion();

        var access = await CreateLoader(Player).GetOwnerOrDefault(mock.ProjectInfo.ProjectId, ThreadDiscussionId);

        access.ShouldNotBeNull().HasAccess.ShouldBeFalse();
    }
}
