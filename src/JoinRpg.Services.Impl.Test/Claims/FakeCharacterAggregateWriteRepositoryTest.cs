using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Тесты на сам фейк write-репозитория: он обязан держать те же инварианты, что боевая реализация.
/// Иначе тесты сервисов пройдут там, где боевой код упадёт — ровно та ошибка, из-за которой в
/// ADR013 мок «врал» про заявки игрока.
/// </summary>
public class FakeCharacterAggregateWriteRepositoryTest : ClaimServiceTestBase
{
    [Fact]
    public async Task CharacterInfoSharesProjectInfoInstanceWithHandle()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        var handle = await WriteRepository.LoadClaimForUpdate(claim.GetId());

        // Конструктор CharacterInfo требует ровно тот же экземпляр ProjectInfo (ADR013).
        ReferenceEquals(handle.CharacterInfo.ProjectInfo, handle.ProjectInfo).ShouldBeTrue();
    }

    [Fact]
    public async Task ClaimInfoIsSameInstanceAsInCharacterInfo()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        var handle = await WriteRepository.LoadClaimForUpdate(claim.GetId());

        var fromAggregate = handle.CharacterInfo.Claims.Single(c => c.ClaimId == claim.GetId());
        ReferenceEquals(handle.CharacterClaimInfo, fromAggregate).ShouldBeTrue();
    }

    [Fact]
    public async Task ClaimSnapshotIsBuiltOverHandleCharacterInfo()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        var handle = await WriteRepository.LoadClaimForUpdate(claim.GetId());

        // ADR021: снимок заявки — над тем же агрегатом, что отдаёт хэндл, а не над перечитанным.
        ReferenceEquals(handle.ClaimSnapshot.Character, handle.CharacterInfo).ShouldBeTrue();
    }

    [Fact]
    public async Task LoadOtherCharacterThrowsWhenCharacterMissing()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var handle = await WriteRepository.LoadClaimForUpdate(claim.GetId());

        _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => handle.LoadOtherCharacter(new CharacterIdentification(ProjectId, 100500)));
    }

    [Fact]
    public async Task LoadOtherCharacterReturnsBothEntityAndInfo()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var other = mock.CreateCharacter("Other character");
        var handle = await WriteRepository.LoadClaimForUpdate(claim.GetId());

        var (entity, info) = await handle.LoadOtherCharacter(other.GetId());

        entity.ShouldBeSameAs(other);
        info.Id.ShouldBe(other.GetId());
        ReferenceEquals(info.ProjectInfo, handle.ProjectInfo).ShouldBeTrue();
    }

    [Fact]
    public async Task LoadClaimForUpdateThrowsWhenClaimMissing()
        => _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => WriteRepository.LoadClaimForUpdate(new ClaimIdentification(ProjectId, 100500)));

    // --- Восстановленные комментарии: те же исходы, что у батча в бою ---

    private static readonly DateTime CommentedAt = new(2026, 7, 1, 7, 0, 0, DateTimeKind.Utc);

    private RestoredClaimCommentRow CommentRow(DataModel.Claim claim, int? commentId, string text = "Текст")
        => new(claim.GetId(), commentId, new UserIdentification(mock.Player.UserId), IsCommentByPlayer: true,
            IsVisibleToPlayer: true, ExtraAction: null, CommentedAt, text);

    [Fact]
    public async Task RestoredComment_WithId_IsInsertedOnce()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        (await WriteRepository.InsertRestoredClaimComment(CommentRow(claim, 500))).ShouldBe(RestoredCommentInsertResult.Inserted);
        (await WriteRepository.InsertRestoredClaimComment(CommentRow(claim, 500, "Другой текст")))
            .ShouldBe(RestoredCommentInsertResult.AlreadyExists);

        var comment = claim.CommentDiscussion.Comments.ShouldHaveSingleItem();
        comment.CommentId.ShouldBe(500);
        comment.CreatedAt.ShouldBe(CommentedAt);
        comment.LastEditTime.ShouldBe(CommentedAt);
        comment.ParentCommentId.ShouldBeNull();
        comment.CommentText.Text.Contents.ShouldBe("Текст");
    }

    [Fact]
    public async Task RestoredComment_IdOfOtherDiscussion_IsTakenElsewhere()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var other = mock.CreateClaim(mock.CreateCharacter("Другой"), mock.Player);
        _ = await WriteRepository.InsertRestoredClaimComment(CommentRow(other, 500));

        (await WriteRepository.InsertRestoredClaimComment(CommentRow(claim, 500))).ShouldBe(RestoredCommentInsertResult.TakenElsewhere);

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(FakeCharacterAggregateWriteRepository.CommentIdentityCurrent)]
    [InlineData(FakeCharacterAggregateWriteRepository.CommentIdentityCurrent + 1)]
    public async Task RestoredComment_IdNotBelowIdentity_IsNotLost(int commentId)
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        (await WriteRepository.InsertRestoredClaimComment(CommentRow(claim, commentId))).ShouldBe(RestoredCommentInsertResult.NotLostId);

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
    }

    [Fact]
    public async Task RestoredComment_WithoutId_GetsNewId_AndRepeatIsRecognizedByAuthorTimeAndText()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        (await WriteRepository.InsertRestoredClaimComment(CommentRow(claim, null))).ShouldBe(RestoredCommentInsertResult.Inserted);
        (await WriteRepository.InsertRestoredClaimComment(CommentRow(claim, null))).ShouldBe(RestoredCommentInsertResult.AlreadyExists);
        // Текст сравнивается побайтно: другой регистр — другой комментарий.
        (await WriteRepository.InsertRestoredClaimComment(CommentRow(claim, null, "текст"))).ShouldBe(RestoredCommentInsertResult.Inserted);

        claim.CommentDiscussion.Comments.Count.ShouldBe(2);
        claim.CommentDiscussion.Comments.ShouldAllBe(c => c.CommentId > FakeCharacterAggregateWriteRepository.CommentIdentityCurrent);
    }

    [Fact]
    public async Task RestoredComment_ClaimMissing_Throws()
        => _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => WriteRepository.InsertRestoredClaimComment(new RestoredClaimCommentRow(
                new ClaimIdentification(ProjectId, 100500), 500, new UserIdentification(mock.Player.UserId),
                true, true, null, CommentedAt, "Текст")));
}
