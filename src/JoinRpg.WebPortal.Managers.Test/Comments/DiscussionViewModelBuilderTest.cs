using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces;
using JoinRpg.Web.Comments;
using JoinRpg.WebPortal.Managers.Comments;

namespace JoinRpg.WebPortal.Managers.Test.Comments;

/// <summary>
/// Модель обсуждения из снимка (ADR026 §3): дерево и порядок, видимость, «новое», тексты, аватарки,
/// способ оплаты.
/// </summary>
public class DiscussionViewModelBuilderTest
{
    private static readonly CommentDiscussionId DiscussionId = new(700);
    private static readonly AvatarIdentification MasterAvatar = new(55);
    private static readonly DateTime Start = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly MockedProject mock = new();
    private readonly FakeCommentDiscussionRepository discussions = new();
    private readonly List<CommentInfo> comments = [];

    private UserIdentification Master => new(mock.Master.UserId);
    private UserIdentification Player => new(mock.Player.UserId);

    private CommentInfo AddComment(
        int id,
        UserIdentification author,
        int? parentId = null,
        bool isVisibleToPlayer = true,
        int minutes = 0,
        CommentExtraAction? extraAction = null,
        CommentFinanceInfo? finance = null)
    {
        var comment = new CommentInfo(
            new CommentId(id),
            parentId is { } parent ? new CommentId(parent) : null,
            author,
            Start.AddMinutes(minutes),
            IsCommentByPlayer: author == Player,
            isVisibleToPlayer,
            extraAction,
            new MarkdownString($"текст **{id}**"),
            finance);
        comments.Add(comment);
        return comment;
    }

    private CommentDiscussionInfo Snapshot()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        return new CommentDiscussionInfo(DiscussionId, new ClaimDiscussionOwner(claim.GetId()), comments);
    }

    private async Task<DiscussionViewModel> Build(UserIdentification currentUser)
    {
        var users = new FakeUserRepository(userId =>
            userId == Master
                ? mock.MasterInfo with { SelectedAvatar = new UserAvatarSource(MasterAvatar, null, "https://example.com/master.png") }
                : mock.TryGetUserInfo(userId));
        var builder = new DiscussionViewModelBuilder(users, discussions, new FakeAvatarLoader(), new FakeCurrentUserAccessor(currentUser));
        return await builder.Build(Snapshot(), mock.ProjectInfo);
    }

    private static IEnumerable<CommentViewModel> Flatten(IEnumerable<CommentViewModel> comments)
        => comments.SelectMany(comment => Flatten(comment.Replies).Prepend(comment));

    private static CommentViewModel Find(DiscussionViewModel model, int id)
        => Flatten(model.RootComments).Single(comment => comment.CommentId == new CommentId(id));

    [Fact]
    public async Task Tree_GroupsRepliesUnderParents_InSnapshotOrder()
    {
        _ = AddComment(1, Master, minutes: 0);
        _ = AddComment(2, Player, minutes: 10);
        // Ответы к 1 — в снимке позже корня 2, а между собой по времени, не по id.
        _ = AddComment(12, Player, parentId: 1, minutes: 30);
        _ = AddComment(11, Master, parentId: 1, minutes: 20);
        _ = AddComment(21, Master, parentId: 11, minutes: 40);

        var model = await Build(Master);

        model.RootComments.Select(c => c.CommentId.Value).ShouldBe([1, 2]);
        var first = model.RootComments[0];
        first.Replies.Select(c => c.CommentId.Value).ShouldBe([11, 12]);
        first.Replies[0].Replies.ShouldHaveSingleItem().CommentId.Value.ShouldBe(21);
        Find(model, 1).Depth.ShouldBe(0);
        Find(model, 11).Depth.ShouldBe(1);
        Find(model, 21).Depth.ShouldBe(2);
    }

    [Fact]
    public async Task HiddenComments_WithReplies_AreNotShownToPlayer()
    {
        _ = AddComment(1, Master);
        _ = AddComment(2, Master, isVisibleToPlayer: false);
        _ = AddComment(3, Master, parentId: 2, isVisibleToPlayer: false);
        _ = AddComment(4, Master, parentId: 1, isVisibleToPlayer: false);

        var model = await Build(Player);

        model.HasMasterAccess.ShouldBeFalse();
        Flatten(model.RootComments).Select(c => c.CommentId.Value).ShouldBe([1]);
    }

    [Fact]
    public async Task HiddenComments_AreShownToMaster()
    {
        _ = AddComment(1, Master);
        _ = AddComment(2, Master, isVisibleToPlayer: false);
        _ = AddComment(3, Master, parentId: 2, isVisibleToPlayer: false);

        var model = await Build(Master);

        model.HasMasterAccess.ShouldBeTrue();
        Flatten(model.RootComments).Select(c => c.CommentId.Value).ShouldBe([1, 2, 3]);
        Find(model, 2).IsVisibleToPlayer.ShouldBeFalse();
    }

    [Fact]
    public async Task IsRead_UpToMaxOfWatermarkAndOwnComments()
    {
        _ = AddComment(1, Master);
        _ = AddComment(2, Master);
        _ = AddComment(3, Player);
        _ = AddComment(4, Master);
        _ = AddComment(5, Master, parentId: 1);
        _ = discussions.AddReadWatermark(DiscussionId, Player, new CommentId(2));

        var model = await Build(Player);

        // Свой комментарий 3 выше отметки 2 — прочитано всё до него, и ответ 5 на старый комментарий новый.
        Find(model, 1).IsRead.ShouldBeTrue();
        Find(model, 2).IsRead.ShouldBeTrue();
        Find(model, 3).IsRead.ShouldBeTrue();
        Find(model, 4).IsRead.ShouldBeFalse();
        Find(model, 5).IsRead.ShouldBeFalse();
    }

    [Fact]
    public async Task IsRead_WithoutWatermarkAndOwnComments_AllNew()
    {
        _ = AddComment(1, Master);
        _ = AddComment(2, Master);

        var model = await Build(Player);

        Flatten(model.RootComments).ShouldAllBe(c => !c.IsRead);
    }

    [Fact]
    public async Task ExtraAction_IsRussianText()
    {
        _ = AddComment(1, Master, extraAction: CommentExtraAction.ApproveByMaster);
        _ = AddComment(2, Master);

        var model = await Build(Master);

        Find(model, 1).ExtraActionText.ShouldBe("Заявка одобрена мастером");
        Find(model, 2).ExtraActionText.ShouldBeNull();
    }

    [Fact]
    public async Task Author_HasLinkAndAvatarUri()
    {
        _ = AddComment(1, Master);
        _ = AddComment(2, Player);

        var model = await Build(Master);

        Find(model, 1).Author.UserId.ShouldBe(Master);
        Find(model, 1).Author.DisplayName.ShouldBe("Master");
        Find(model, 1).AuthorAvatarUri.ShouldBe(FakeAvatarLoader.UriFor(MasterAvatar));
        // У игрока мока аватарки нет — и адреса нет, а не заглушка.
        Find(model, 2).AuthorAvatarUri.ShouldBeNull();
    }

    [Fact]
    public async Task Text_IsSanitizedHtml()
    {
        _ = AddComment(1, Master);

        var model = await Build(Master);

        Find(model, 1).TextHtml.ShouldContain("<strong>1</strong>");
    }

    [Fact]
    public async Task Finance_ResolvesPaymentTypeFromProjectInfo()
    {
        var paymentType = mock.CreateCashPaymentType();
        var paymentTypeId = new PaymentTypeIdentification(mock.ProjectInfo.ProjectId, paymentType.PaymentTypeId);
        _ = AddComment(1, Player, finance: new CommentFinanceInfo(
            500, FinanceOperationType.Submit, FinanceOperationState.Proposed, Start, paymentTypeId));
        _ = AddComment(2, Master, finance: new CommentFinanceInfo(
            -200, FinanceOperationType.TransferTo, FinanceOperationState.Approved, Start, PaymentType: null));

        var model = await Build(Master);

        var payment = Find(model, 1).Finance.ShouldNotBeNull();
        payment.PaymentTypeName.ShouldBe("Наличными — Master");
        payment.OperationText.ShouldBe("Оплата денег игроком");
        payment.OperationTypeName.ShouldBe("Оплата");
        payment.IsProposed.ShouldBeTrue();
        payment.CanModerate.ShouldBeTrue();

        var transfer = Find(model, 2).Finance.ShouldNotBeNull();
        transfer.PaymentTypeName.ShouldBeNull();
        transfer.OperationText.ShouldBe("Перевод денег другому игроку");
        transfer.CanModerate.ShouldBeFalse();
    }

    [Fact]
    public async Task Finance_PlayerCannotModerate()
    {
        var paymentType = mock.CreateCashPaymentType();
        _ = AddComment(1, Player, finance: new CommentFinanceInfo(
            500, FinanceOperationType.Submit, FinanceOperationState.Proposed, Start,
            new PaymentTypeIdentification(mock.ProjectInfo.ProjectId, paymentType.PaymentTypeId)));

        var model = await Build(Player);

        Find(model, 1).Finance.ShouldNotBeNull().CanModerate.ShouldBeFalse();
    }

    [Fact]
    public async Task CanConceal_OnlyMasterCommentVisibleWithoutReplies_ForMaster()
    {
        _ = AddComment(1, Master);
        _ = AddComment(2, Master);
        _ = AddComment(3, Player, parentId: 2);
        _ = AddComment(4, Master, isVisibleToPlayer: false);

        var master = await Build(Master);
        var player = await Build(Player);

        Find(master, 1).CanConceal.ShouldBeTrue();
        Find(master, 2).CanConceal.ShouldBeFalse();
        Find(master, 3).CanConceal.ShouldBeFalse();
        Find(master, 4).CanConceal.ShouldBeFalse();
        Find(player, 1).CanConceal.ShouldBeFalse();
    }

    [Fact]
    public async Task DiscussionOfOtherProject_Throws()
    {
        _ = AddComment(1, Master);
        var builder = new DiscussionViewModelBuilder(
            FakeUserRepository.MustNotBeCalled("обсуждение чужого проекта"),
            discussions,
            new FakeAvatarLoader(),
            new FakeCurrentUserAccessor(Master));
        var otherProject = new ProjectIdentification(mock.ProjectInfo.ProjectId.Value + 1);
        var foreignDiscussion = new CommentDiscussionInfo(
            DiscussionId,
            new ClaimDiscussionOwner(new ClaimIdentification(otherProject, 1)),
            comments);

        _ = await Should.ThrowAsync<ArgumentException>(() => builder.Build(foreignDiscussion, mock.ProjectInfo));
    }

    private sealed class FakeAvatarLoader : IAvatarLoader
    {
        public static Uri UriFor(AvatarIdentification avatarId) => new($"https://avatars.example.com/{avatarId.Value}.png");

        public Task<AvatarInfo> GetAvatar(AvatarIdentification userAvatarId, int recommendedSize)
            => Task.FromResult(new AvatarInfo(UriFor(userAvatarId), recommendedSize));
    }
}
