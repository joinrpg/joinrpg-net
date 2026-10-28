using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories.Comments;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>
/// Юнит-тесты чистого маппинга <see cref="CommentDiscussionMapper"/> (ADR026 §2). БД не используется.
/// </summary>
public class CommentDiscussionMapperTest
{
    private const int ProjectIntId = 7;
    private static readonly ProjectIdentification ProjectId = new(ProjectIntId);
    private static readonly DateTime SomeDate = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static CommentDiscussionRow MakeRow(
        int? claimId = 10,
        int? forumThreadId = null,
        int? forumThreadCharacterGroupId = null,
        bool? forumThreadIsVisibleToPlayer = null,
        int ownerProjectId = ProjectIntId,
        IEnumerable<CommentRow>? comments = null)
        => new()
        {
            CommentDiscussionId = 3,
            ProjectId = ProjectIntId,
            ClaimId = claimId,
            ClaimProjectId = claimId is null ? null : ownerProjectId,
            ForumThreadId = forumThreadId,
            ForumThreadProjectId = forumThreadId is null ? null : ownerProjectId,
            ForumThreadCharacterGroupId = forumThreadCharacterGroupId,
            ForumThreadIsVisibleToPlayer = forumThreadIsVisibleToPlayer,
            Comments = comments ?? [],
        };

    private static CommentRow MakeCommentRow(
        int commentId = 1,
        int? parentCommentId = null,
        DateTime? createdAt = null,
        string? text = "текст",
        int? financeMoneyAmount = null,
        FinanceOperationType? financeOperationType = null,
        FinanceOperationState? financeState = null,
        DateTime? financeOperationDate = null,
        int? financePaymentTypeId = null)
        => new()
        {
            CommentId = commentId,
            ParentCommentId = parentCommentId,
            AuthorUserId = 5,
            CreatedAt = createdAt ?? SomeDate,
            IsCommentByPlayer = true,
            IsVisibleToPlayer = false,
            ExtraAction = CommentExtraAction.ApproveByMaster,
            Text = text,
            FinanceMoneyAmount = financeMoneyAmount,
            FinanceOperationType = financeOperationType,
            FinanceState = financeState,
            FinanceOperationDate = financeOperationDate,
            FinancePaymentTypeId = financePaymentTypeId,
        };

    [Fact]
    public void ClaimOwner()
    {
        var discussion = CommentDiscussionMapper.Map(MakeRow(claimId: 10));

        discussion.Id.ShouldBe(new CommentDiscussionId(3));
        discussion.Owner.ShouldBe(new ClaimDiscussionOwner(new ClaimIdentification(ProjectId, 10)));
    }

    [Fact]
    public void ForumOwner()
    {
        var owner = CommentDiscussionMapper.MapOwner(MakeRow(
            claimId: null,
            forumThreadId: 20,
            forumThreadCharacterGroupId: 30,
            forumThreadIsVisibleToPlayer: false));

        owner.ShouldBe(new ForumDiscussionOwner(
            new ForumThreadIdentification(ProjectId, 20),
            new CharacterGroupIdentification(ProjectId, 30),
            IsVisibleToPlayer: false));
    }

    [Fact]
    public void DiscussionWithoutOwnerIsInconsistent()
        => Should.Throw<InvalidOperationException>(() => CommentDiscussionMapper.MapOwner(MakeRow(claimId: null)));

    [Fact]
    public void DiscussionWithTwoOwnersIsInconsistent()
        => Should.Throw<InvalidOperationException>(() => CommentDiscussionMapper.MapOwner(MakeRow(
            claimId: 10,
            forumThreadId: 20,
            forumThreadCharacterGroupId: 30,
            forumThreadIsVisibleToPlayer: true)));

    [Fact]
    public void ClaimFromAnotherProjectIsInconsistent()
        => Should.Throw<InvalidOperationException>(() => CommentDiscussionMapper.MapOwner(MakeRow(
            claimId: 10,
            ownerProjectId: ProjectIntId + 1)));

    [Fact]
    public void ForumThreadFromAnotherProjectIsInconsistent()
        => Should.Throw<InvalidOperationException>(() => CommentDiscussionMapper.MapOwner(MakeRow(
            claimId: null,
            forumThreadId: 20,
            forumThreadCharacterGroupId: 30,
            forumThreadIsVisibleToPlayer: true,
            ownerProjectId: ProjectIntId + 1)));

    [Fact]
    public void CommentScalars()
    {
        var comment = CommentDiscussionMapper.Map(MakeRow(comments: [MakeCommentRow(commentId: 2, parentCommentId: 1)]))
            .Comments.ShouldHaveSingleItem();

        comment.ShouldBe(new CommentInfo(
            new CommentId(2),
            new CommentId(1),
            new UserIdentification(5),
            SomeDate,
            IsCommentByPlayer: true,
            IsVisibleToPlayer: false,
            CommentExtraAction.ApproveByMaster,
            new MarkdownString("текст"),
            Finance: null));
    }

    [Fact]
    public void RootCommentHasNoParent()
        => CommentDiscussionMapper.Map(MakeRow(comments: [MakeCommentRow(parentCommentId: null)]))
            .Comments.ShouldHaveSingleItem().ParentCommentId.ShouldBeNull();

    [Fact]
    public void MissingTextIsNull()
        => CommentDiscussionMapper.Map(MakeRow(comments: [MakeCommentRow(text: null)]))
            .Comments.ShouldHaveSingleItem().Text.ShouldBeNull();

    [Fact]
    public void FinanceWithPaymentType()
    {
        var operationDate = new DateTime(2026, 9, 30);
        var finance = CommentDiscussionMapper.Map(MakeRow(comments: [MakeCommentRow(
                financeMoneyAmount: 1500,
                financeOperationType: FinanceOperationType.Submit,
                financeState: FinanceOperationState.Proposed,
                financeOperationDate: operationDate,
                financePaymentTypeId: 40)]))
            .Comments.ShouldHaveSingleItem().Finance;

        finance.ShouldBe(new CommentFinanceInfo(
            1500,
            FinanceOperationType.Submit,
            FinanceOperationState.Proposed,
            operationDate,
            new PaymentTypeIdentification(ProjectId, 40)));
    }

    [Fact]
    public void FinanceWithoutPaymentType()
        => CommentDiscussionMapper.Map(MakeRow(comments: [MakeCommentRow(
                financeMoneyAmount: 0,
                financeOperationType: FinanceOperationType.PreferentialFeeRequest,
                financeState: FinanceOperationState.Proposed,
                financeOperationDate: SomeDate,
                financePaymentTypeId: null)]))
            .Comments.ShouldHaveSingleItem().Finance.ShouldNotBeNull().PaymentType.ShouldBeNull();

    [Fact]
    public void AllCommentsAreMapped()
    {
        // Порядок задаёт сам CommentDiscussionInfo (см. CommentDiscussionInfoTest), здесь — только состав.
        var discussion = CommentDiscussionMapper.Map(MakeRow(comments:
        [
            MakeCommentRow(commentId: 5, createdAt: SomeDate.AddMinutes(1)),
            MakeCommentRow(commentId: 9, createdAt: SomeDate),
            MakeCommentRow(commentId: 4, createdAt: SomeDate),
        ]));

        discussion.Comments.Select(comment => comment.Id.Value).ShouldBe([5, 9, 4], ignoreOrder: true);
    }
}
