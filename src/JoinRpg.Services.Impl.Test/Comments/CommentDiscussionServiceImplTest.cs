using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Comments;
using JoinRpg.Services.Impl.Test.Claims;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Comments;

/// <summary>
/// Общие правила единого входа обсуждения (ADR026 §5) и диспатч по владельцу. Операция заявки —
/// настоящая (<see cref="ClaimServiceImpl"/> поверх фейков), чтобы побочные эффекты заявки были видны и
/// через новый вход; операция форума — записывающий фейк: правила проверяются до неё.
/// </summary>
public class CommentDiscussionServiceImplTest : ClaimServiceTestBase
{
    private static readonly CommentDiscussionId ClaimDiscussionId = new(100);
    private static readonly CommentDiscussionId ThreadDiscussionId = new(200);
    private static readonly CommentDiscussionId OtherDiscussionId = new(300);

    private readonly FakeCommentDiscussionRepository discussionRepository = new();
    private readonly RecordingForumCommentOperation forumOperation = new();

    private CommentDiscussionServiceImpl CreateService(int? currentUserId = null)
    {
        var currentUser = CreateCurrentUser(currentUserId);
        var claimService = new ClaimServiceImpl(
            unitOfWork,
            currentUser,
            metadataRepository,
            new FakeProblemValidator(),
            NullLogger<CharacterServiceImpl>.Instance,
            CreatePropsService(currentUserId),
            impersonateAccessor,
            CreateUserFieldValidator(),
            CreateFieldSetupService(currentUserId));
        return new CommentDiscussionServiceImpl(
            discussionRepository,
            metadataRepository,
            currentUser,
            unitOfWork,
            claimService,
            forumOperation);
    }

    private Claim CreateClaim()
    {
        var claim = CreateClaimWithoutDiscussion();
        RegisterClaimDiscussion(claim);
        return claim;
    }

    private Claim CreateClaimWithoutDiscussion()
    {
        var claim = mock.CreateClaim(mock.CreateCharacter("Вася"), mock.Player);
        claim.ClaimStatus = ClaimStatus.AddedByUser;
        mock.ReInitProjectInfo();
        return claim;
    }

    private void RegisterClaimDiscussion(Claim claim, params CommentInfo[] comments)
        => _ = discussionRepository.Add(new CommentDiscussionInfo(ClaimDiscussionId, new ClaimDiscussionOwner(claim.GetId()), comments));

    private ForumThreadIdentification CreateThread(params CommentInfo[] comments)
    {
        var threadId = new ForumThreadIdentification(ProjectId, 1);
        var owner = new ForumDiscussionOwner(threadId, mock.ProjectInfo.GroupTree.RootGroupId, IsVisibleToPlayer: true);
        _ = discussionRepository.Add(new CommentDiscussionInfo(ThreadDiscussionId, owner, comments));
        return threadId;
    }

    /// <summary>Скрытый от игрока комментарий мастера — в снимке и, для заявки, в EF-графе мока.</summary>
    private static CommentInfo HiddenComment(int commentId)
        => new(new CommentId(commentId), ParentCommentId: null, new UserIdentification(2), DateTime.UtcNow,
            IsCommentByPlayer: false, IsVisibleToPlayer: false, ExtraAction: null, Text: null, Finance: null);

    private void ArchiveProject()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();
    }

    private void BlockProject()
    {
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
    }

    #region Диспатч

    [Fact]
    public async Task AddComment_ToClaim_GoesThroughClaimOperation_WithClaimSideEffects()
    {
        var claim = CreateClaim();

        await CreateService().AddComment(ClaimDiscussionId, parentCommentId: null, isVisibleToPlayer: true, "как дела?");

        _ = claim.CommentDiscussion.Comments.ShouldHaveSingleItem();
        // Побочные эффекты заявки остались: видимый ответ мастера переводит заявку в обсуждение.
        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
        claim.LastVisibleMasterCommentBy_Id.ShouldBe(mock.Master.UserId);
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
        forumOperation.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddComment_ToThread_GoesThroughForumOperation()
    {
        var threadId = CreateThread(HiddenComment(10));

        await CreateService().AddComment(ThreadDiscussionId, new CommentId(10), isVisibleToPlayer: false, "скрытый ответ");

        forumOperation.Calls.ShouldHaveSingleItem().ShouldBe((threadId, (int?)10, false, "скрытый ответ"));
    }

    [Fact]
    public async Task AddComment_ToUnknownDiscussion_Throws()
        => _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => CreateService().AddComment(OtherDiscussionId, parentCommentId: null, isVisibleToPlayer: true, "куда это?"));

    #endregion

    #region Видимый ответ на скрытый

    [Fact]
    public async Task AddComment_VisibleReplyToHidden_InClaim_Throws_AndDoesNotSave()
    {
        var claim = CreateClaimWithoutDiscussion();
        var hidden = mock.CreateComment(claim, "только для мастеров", isVisibleToPlayer: false);
        // Снимок — тот же комментарий, что и в EF-графе.
        RegisterClaimDiscussion(claim, HiddenComment(hidden.CommentId));

        _ = await Should.ThrowAsync<VisibleReplyToHiddenCommentException>(
            () => CreateService().AddComment(ClaimDiscussionId, new CommentId(hidden.CommentId), isVisibleToPlayer: true, "отвечаю"));

        claim.CommentDiscussion.Comments.ShouldHaveSingleItem().ShouldBe(hidden);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    /// <summary>Изменение поведения: раньше форум молча делал такой ответ скрытым.</summary>
    [Fact]
    public async Task AddComment_VisibleReplyToHidden_InThread_Throws()
    {
        _ = CreateThread(HiddenComment(10));

        _ = await Should.ThrowAsync<VisibleReplyToHiddenCommentException>(
            () => CreateService().AddComment(ThreadDiscussionId, new CommentId(10), isVisibleToPlayer: true, "отвечаю"));

        forumOperation.Calls.ShouldBeEmpty();
    }

    #endregion

    #region Скрыть может только мастер

    /// <summary>Изменение поведения: раньше сервис молча делал комментарий видимым.</summary>
    [Fact]
    public async Task AddComment_PlayerAsksToHide_InClaim_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).AddComment(ClaimDiscussionId, parentCommentId: null, isVisibleToPlayer: false, "спрячьте"));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddComment_PlayerAsksToHide_InThread_Throws()
    {
        _ = CreateThread();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).AddComment(ThreadDiscussionId, parentCommentId: null, isVisibleToPlayer: false, "спрячьте"));

        forumOperation.Calls.ShouldBeEmpty();
    }

    #endregion

    #region Родитель из другого обсуждения

    [Fact]
    public async Task AddComment_ParentFromOtherDiscussion_InClaim_Throws()
    {
        var claim = CreateClaim();
        _ = CreateThread(HiddenComment(10));

        _ = await Should.ThrowAsync<ParentCommentNotFoundException>(
            () => CreateService().AddComment(ClaimDiscussionId, new CommentId(10), isVisibleToPlayer: false, "отвечаю"));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task AddComment_ParentFromOtherDiscussion_InThread_Throws()
    {
        _ = CreateThread();
        _ = discussionRepository.Add(new CommentDiscussionInfo(
            OtherDiscussionId,
            new ForumDiscussionOwner(new ForumThreadIdentification(ProjectId, 2), mock.ProjectInfo.GroupTree.RootGroupId, IsVisibleToPlayer: true),
            [HiddenComment(20)]));

        _ = await Should.ThrowAsync<ParentCommentNotFoundException>(
            () => CreateService().AddComment(ThreadDiscussionId, new CommentId(20), isVisibleToPlayer: false, "отвечаю"));

        forumOperation.Calls.ShouldBeEmpty();
    }

    /// <summary>
    /// Постороннему правила родителя не отвечают раньше проверки доступа: иначе по коду отказа он
    /// узнал бы, есть ли в чужом обсуждении такой комментарий и скрыт ли он от игрока.
    /// </summary>
    [Fact]
    public async Task AddComment_ByStranger_WithHiddenOrMissingParent_IsRejectedByAccess()
    {
        var otherPlayer = new User { UserId = 777, PrefferedName = "Чужой игрок", Email = "stranger@example.com", Claims = [] };
        var claim = mock.CreateClaim(mock.CreateCharacter("Чужой"), otherPlayer);
        claim.ClaimStatus = ClaimStatus.AddedByUser;
        mock.ReInitProjectInfo();
        var hidden = mock.CreateComment(claim, "только для мастеров", isVisibleToPlayer: false);
        RegisterClaimDiscussion(claim, HiddenComment(hidden.CommentId));

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).AddComment(ClaimDiscussionId, new CommentId(hidden.CommentId), isVisibleToPlayer: true, "а вот и я"));
        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).AddComment(ClaimDiscussionId, new CommentId(12345), isVisibleToPlayer: true, "а вот и я"));

        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion

    #region Архив и блокировка

    /// <summary>Разница сохранена: заявку в архиве комментировать можно (ADR014).</summary>
    [Fact]
    public async Task AddComment_InArchivedProject_ToClaim_Works()
    {
        var claim = CreateClaim();
        ArchiveProject();

        await CreateService().AddComment(ClaimDiscussionId, parentCommentId: null, isVisibleToPlayer: true, "игра кончилась, обсудим");

        _ = claim.CommentDiscussion.Comments.ShouldHaveSingleItem();
        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>Разница сохранена: тему в архиве комментировать нельзя.</summary>
    [Fact]
    public async Task AddComment_InArchivedProject_ToThread_Throws()
    {
        _ = CreateThread();
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().AddComment(ThreadDiscussionId, parentCommentId: null, isVisibleToPlayer: true, "игра кончилась"));

        forumOperation.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddComment_InBlockedProject_ToClaim_Throws()
    {
        var claim = CreateClaim();
        BlockProject();

        _ = await Should.ThrowAsync<ProjectBlockedException>(
            () => CreateService().AddComment(ClaimDiscussionId, parentCommentId: null, isVisibleToPlayer: true, "в блокировке"));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task AddComment_InBlockedProject_ToThread_Throws()
    {
        _ = CreateThread();
        BlockProject();

        _ = await Should.ThrowAsync<ProjectBlockedException>(
            () => CreateService().AddComment(ThreadDiscussionId, parentCommentId: null, isVisibleToPlayer: true, "в блокировке"));

        forumOperation.Calls.ShouldBeEmpty();
    }

    #endregion

    #region Отметка прочтения

    [Fact]
    public async Task UpdateReadCommentWatermark_WritesWatermark_WithOwnerProject()
    {
        _ = CreateThread();
        var watermarks = new List<ReadCommentWatermark>();
        unitOfWork.UseDbSet(watermarks);

        await CreateService().UpdateReadCommentWatermark(ThreadDiscussionId, new CommentId(42));

        var watermark = watermarks.ShouldHaveSingleItem();
        watermark.CommentDiscussionId.ShouldBe(ThreadDiscussionId.Value);
        watermark.ProjectId.ShouldBe(ProjectId.Value);
        watermark.UserId.ShouldBe(mock.Master.UserId);
        watermark.CommentId.ShouldBe(42);
        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// ADR023: просмотр в заблокированном проекте работает, но отметка молча не пишется. Набор
    /// отметок не подключён — обращение к нему уронило бы тест.
    /// </summary>
    [Fact]
    public async Task UpdateReadCommentWatermark_InBlockedProject_SilentlyDoesNothing()
    {
        _ = CreateThread();
        BlockProject();

        await CreateService().UpdateReadCommentWatermark(ThreadDiscussionId, new CommentId(42));

        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion

    private sealed class RecordingForumCommentOperation : IForumCommentOperation
    {
        public List<(ForumThreadIdentification ThreadId, int? ParentCommentId, bool IsVisibleToPlayer, string CommentText)> Calls { get; } = [];

        public Task AddComment(ForumThreadIdentification forumThreadId, int? parentCommentId, bool isVisibleToPlayer, string commentText)
        {
            Calls.Add((forumThreadId, parentCommentId, isVisibleToPlayer, commentText));
            return Task.CompletedTask;
        }
    }
}
