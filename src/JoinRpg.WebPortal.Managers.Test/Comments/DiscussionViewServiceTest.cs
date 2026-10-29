using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Comments;
using JoinRpg.WebPortal.Managers.Comments;

namespace JoinRpg.WebPortal.Managers.Test.Comments;

/// <summary>
/// Показ обсуждения (ADR026, шаги 6 и 8): модель строится до сдвига отметки прочтения, отметка
/// сдвигается только при непрочитанном и на последний комментарий обсуждения.
/// </summary>
public class DiscussionViewServiceTest
{
    private static readonly CommentDiscussionId DiscussionId = new(700);
    private static readonly DateTime Start = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly MockedProject mock = new();
    private readonly FakeCommentDiscussionRepository discussions = new();
    private readonly List<CommentInfo> comments = [];

    private UserIdentification Master => new(mock.Master.UserId);
    private UserIdentification Player => new(mock.Player.UserId);

    private void AddComment(int id, UserIdentification author, int? parentId = null, bool isVisibleToPlayer = true)
        => comments.Add(new CommentInfo(
            new CommentId(id),
            parentId is { } parent ? new CommentId(parent) : null,
            author,
            Start.AddMinutes(id),
            IsCommentByPlayer: author == Player,
            isVisibleToPlayer,
            ExtraAction: null,
            new MarkdownString($"текст {id}"),
            Finance: null));

    private async Task<(DiscussionViewModel? Model, WatermarkRecordingService Service)> Show(
        UserIdentification currentUser,
        bool discussionExists = true)
    {
        if (discussionExists)
        {
            var claim = mock.CreateClaim(mock.Character, mock.Player);
            _ = discussions.Add(new CommentDiscussionInfo(DiscussionId, new ClaimDiscussionOwner(claim.GetId()), comments));
        }
        var service = new WatermarkRecordingService(discussions, currentUser);
        var viewService = new DiscussionViewService(
            discussions,
            new FakeUserRepository(mock.TryGetUserInfo),
            new FakeAvatarLoader(),
            new FakeCurrentUserAccessor(currentUser),
            service);
        return (await viewService.ShowDiscussion(DiscussionId, mock.ProjectInfo), service);
    }

    private static IEnumerable<CommentViewModel> Flatten(IEnumerable<CommentViewModel> comments)
        => comments.SelectMany(comment => Flatten(comment.Replies).Prepend(comment));

    [Fact]
    public async Task UnreadReply_StaysNew_AndWatermarkMovesToLastComment()
    {
        AddComment(1, Master);
        AddComment(3, Master);
        AddComment(2, Master, parentId: 1);

        var (model, service) = await Show(Player);

        // Сервис отметки пишет её в тот же фейк, из которого читает построитель: если бы модель
        // строилась после сдвига, всё было бы прочитанным.
        Flatten(model.ShouldNotBeNull().RootComments).ShouldAllBe(comment => !comment.IsRead);
        service.Calls.ShouldHaveSingleItem().ShouldBe((DiscussionId, new CommentId(3)));
    }

    [Fact]
    public async Task AllRead_WatermarkIsNotTouched()
    {
        AddComment(1, Master);
        AddComment(2, Player);
        _ = discussions.AddReadWatermark(DiscussionId, Master, new CommentId(2));

        var (model, service) = await Show(Master);

        model.ShouldNotBeNull().HasUnread().ShouldBeFalse();
        service.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task OnlyHiddenCommentsAreNew_ForPlayer_WatermarkIsNotTouched()
    {
        AddComment(1, Player);
        AddComment(2, Master, isVisibleToPlayer: false);

        var (_, service) = await Show(Player);

        service.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task HiddenComment_CountsForMaxId_WhenSomethingVisibleIsNew()
    {
        AddComment(1, Master);
        AddComment(2, Master, isVisibleToPlayer: false);

        var (_, service) = await Show(Player);

        service.Calls.ShouldHaveSingleItem().ShouldBe((DiscussionId, new CommentId(2)));
    }

    [Fact]
    public async Task MissingDiscussion_IsNull()
    {
        var (model, service) = await Show(Master, discussionExists: false);

        model.ShouldBeNull();
        service.Calls.ShouldBeEmpty();
    }

    private sealed class FakeAvatarLoader : IAvatarLoader
    {
        public Task<AvatarInfo> GetAvatar(AvatarIdentification userAvatarId, int recommendedSize)
            => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<AvatarIdentification, AvatarInfo>> GetAvatars(
            IReadOnlyCollection<AvatarIdentification> avatarIds,
            int recommendedSize)
            => Task.FromResult<IReadOnlyDictionary<AvatarIdentification, AvatarInfo>>(
                avatarIds.Distinct().ToDictionary(id => id, id => new AvatarInfo(new Uri($"https://avatars.example.com/{id.Value}.png"), recommendedSize)));
    }

    /// <summary>Запоминает сдвиги отметки и сразу пишет их туда, откуда читает построитель.</summary>
    private sealed class WatermarkRecordingService(FakeCommentDiscussionRepository discussions, UserIdentification currentUser)
        : ICommentDiscussionService
    {
        public List<(CommentDiscussionId DiscussionId, CommentId CommentId)> Calls { get; } = [];

        public Task UpdateReadCommentWatermark(CommentDiscussionId discussionId, CommentId maxCommentId)
        {
            Calls.Add((discussionId, maxCommentId));
            _ = discussions.AddReadWatermark(discussionId, currentUser, maxCommentId);
            return Task.CompletedTask;
        }

        public Task AddComment(CommentDiscussionId discussionId, CommentId? parentCommentId, bool isVisibleToPlayer, string commentText)
            => throw new NotSupportedException();

        public Task ConcealComment(CommentId commentId) => throw new NotSupportedException();
    }
}
