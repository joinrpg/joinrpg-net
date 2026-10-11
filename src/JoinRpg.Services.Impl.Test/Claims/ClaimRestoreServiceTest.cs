using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Interfaces.Drp;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Восстановление заявок по базе уведомлений (DRP, #5481): только в заблокированном проекте, мимо
/// таблицы переходов, без уведомлений, с одной скрытой записью в истории.
/// </summary>
public class ClaimRestoreServiceTest : ClaimServiceTestBase
{
    private static readonly DateTimeOffset StatusChangedAt = new(2026, 9, 15, 12, 30, 0, TimeSpan.FromHours(3));

    private ClaimRestoreService CreateService(int? currentUserId = null) => new(CreatePropsService(currentUserId));

    private void BlockProject()
    {
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
    }

    private Claim CreateClaim(ClaimStatus status, string characterName = "Вася")
    {
        var character = mock.CreateCharacter(characterName);
        var claim = mock.CreateClaim(character, mock.Player);
        claim.ClaimStatus = status;
        BlockProject();
        return claim;
    }

    private static RestoredClaimTarget Target(
        CharacterIdentification? character = null,
        ClaimStatus? status = null,
        DateTimeOffset? statusChangedAt = null,
        UserIdentification? responsibleMaster = null,
        int? manualFee = null)
        => new(character, status, statusChangedAt, responsibleMaster, manualFee);

    private static List<Comment> RestoreComments(Claim claim)
        => [.. claim.CommentDiscussion.Comments.Where(c => c.ExtraAction == CommentExtraAction.RestoredFromNotifications)];

    [Fact]
    public async Task ActiveProject_Throws_AndNothingIsSaved()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateClaim(character, mock.Player);
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt)));

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        SaveChangesCallCount.ShouldBe(0);
        claim.CommentDiscussion.Comments.ShouldBeEmpty();
    }

    [Fact]
    public async Task Status_BypassesTransitionTable_AndStampsEventDate()
    {
        var claim = CreateClaim(ClaimStatus.DeclinedByMaster);
        claim.Character.IsHot = true;

        await CreateService().ApplyRestoredState(
            claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.MasterAcceptedDate.ShouldBe(StatusChangedAt.UtcDateTime);
        claim.MasterAcceptedDate!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        claim.Character.ApprovedClaimId.ShouldBe(claim.ClaimId);
        claim.Character.IsHot.ShouldBeFalse();
        claim.Character.InGame.ShouldBeFalse();
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task LeavingApproved_ClearsApprovedClaim()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        BlockProject();

        await CreateService().ApplyRestoredState(
            claim.GetId(), Target(status: ClaimStatus.DeclinedByMaster, statusChangedAt: StatusChangedAt));

        claim.ClaimStatus.ShouldBe(ClaimStatus.DeclinedByMaster);
        character.ApprovedClaimId.ShouldBeNull();
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task CheckedIn_SetsInGame()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        BlockProject();

        await CreateService().ApplyRestoredState(
            claim.GetId(), Target(status: ClaimStatus.CheckedIn, statusChangedAt: StatusChangedAt));

        claim.ClaimStatus.ShouldBe(ClaimStatus.CheckedIn);
        character.ApprovedClaimId.ShouldBe(claim.ClaimId);
        character.InGame.ShouldBeTrue();
    }

    [Fact]
    public async Task Approve_WhenCharacterHasOtherApprovedClaim_Throws()
    {
        var character = mock.CreateCharacter("Вася");
        var other = mock.CreateApprovedClaim(character, mock.CreateMaster("Другой игрок"));
        var claim = mock.CreateClaim(character, mock.Player);
        claim.ClaimStatus = ClaimStatus.Discussed;
        BlockProject();

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().ApplyRestoredState(
                claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt)));

        character.ApprovedClaimId.ShouldBe(other.ClaimId);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Move_ChangesCharacter_AndMovesApprovedLink()
    {
        var oldCharacter = mock.CreateCharacter("Вася");
        var newCharacter = mock.CreateCharacter("Петя");
        var claim = mock.CreateApprovedClaim(oldCharacter, mock.Player);
        BlockProject();

        await CreateService().ApplyRestoredState(claim.GetId(), Target(character: newCharacter.GetId()));

        claim.CharacterId.ShouldBe(newCharacter.CharacterId);
        oldCharacter.ApprovedClaimId.ShouldBeNull();
        newCharacter.ApprovedClaimId.ShouldBe(claim.ClaimId);
        SaveChangesCallCount.ShouldBe(1);
        RestoreComments(claim).Count.ShouldBe(1);
    }

    [Fact]
    public async Task ResponsibleMaster_IsChanged()
    {
        var master2 = mock.CreateMaster();
        var claim = CreateClaim(ClaimStatus.Discussed);

        await CreateService().ApplyRestoredState(claim.GetId(), Target(responsibleMaster: new UserIdentification(master2.UserId)));

        claim.ResponsibleMasterUserId.ShouldBe(master2.UserId);
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task ResponsibleMaster_NotAMaster_Throws()
    {
        var claim = CreateClaim(ClaimStatus.Discussed);

        // Не «нет доступа» у восстанавливающего, а понятная ошибка про ответственного.
        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().ApplyRestoredState(claim.GetId(), Target(responsibleMaster: new UserIdentification(mock.Player.UserId))));

        claim.ResponsibleMasterUserId.ShouldBe(mock.Master.UserId);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task ManualFee_IsSet()
    {
        var claim = CreateClaim(ClaimStatus.Approved);

        await CreateService().ApplyRestoredState(claim.GetId(), Target(manualFee: 1500));

        claim.CurrentFee.ShouldBe(1500);
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task WritesSingleHiddenHistoryRecord_WithoutNotifications()
    {
        var master2 = mock.CreateMaster();
        var claim = CreateClaim(ClaimStatus.Discussed);

        await CreateService().ApplyRestoredState(
            claim.GetId(),
            Target(
                status: ClaimStatus.OnHold,
                statusChangedAt: StatusChangedAt,
                responsibleMaster: new UserIdentification(master2.UserId),
                manualFee: 700));

        var comment = claim.CommentDiscussion.Comments.ShouldHaveSingleItem();
        comment.ExtraAction.ShouldBe(CommentExtraAction.RestoredFromNotifications);
        comment.IsVisibleToPlayer.ShouldBeFalse();
        claimNotifications.Sent.ShouldBeEmpty();
        SentInOrder.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task SameState_IsNoOp()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        claim.CurrentFee = 1000;
        BlockProject();
        var updatedAt = character.UpdatedAt;

        await CreateService().ApplyRestoredState(
            claim.GetId(),
            Target(
                character: character.GetId(),
                status: ClaimStatus.Approved,
                statusChangedAt: StatusChangedAt,
                responsibleMaster: new UserIdentification(mock.Master.UserId),
                manualFee: 1000));

        SaveChangesCallCount.ShouldBe(0);
        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        claimNotifications.Sent.ShouldBeEmpty();
        // Отслеживаемые сущности не тронуты: иначе следующее сохранение в том же контексте их записало бы.
        character.UpdatedAt.ShouldBe(updatedAt);
    }

    [Fact]
    public async Task CheckedInToDeclined_ClearsLinkAndInGame()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        claim.ClaimStatus = ClaimStatus.CheckedIn;
        character.InGame = true;
        BlockProject();

        await CreateService().ApplyRestoredState(
            claim.GetId(), Target(status: ClaimStatus.DeclinedByMaster, statusChangedAt: StatusChangedAt));

        character.ApprovedClaimId.ShouldBeNull();
        character.InGame.ShouldBeFalse();
        claim.MasterDeclinedDate.ShouldBe(StatusChangedAt.UtcDateTime);
    }

    [Fact]
    public async Task MoveAndApprove_LinksNewCharacterOnly()
    {
        var oldCharacter = mock.CreateCharacter("Вася");
        var newCharacter = mock.CreateCharacter("Петя");
        var claim = mock.CreateClaim(oldCharacter, mock.Player);
        claim.ClaimStatus = ClaimStatus.DeclinedByMaster;
        BlockProject();

        await CreateService().ApplyRestoredState(
            claim.GetId(), Target(character: newCharacter.GetId(), status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));

        claim.CharacterId.ShouldBe(newCharacter.CharacterId);
        oldCharacter.ApprovedClaimId.ShouldBeNull();
        newCharacter.ApprovedClaimId.ShouldBe(claim.ClaimId);
        RestoreComments(claim).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Approve_OnSlot_Throws()
    {
        var slot = mock.CreateCharacter("Слот");
        slot.CharacterType = CharacterType.Slot;
        var claim = mock.CreateClaim(slot, mock.Player);
        BlockProject();

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt)));

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        slot.ApprovedClaimId.ShouldBeNull();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestoringDeclinedClaim_RevivesDeletedCharacter()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateClaim(character, mock.Player);
        claim.ClaimStatus = ClaimStatus.DeclinedByMaster;
        claim.ClaimDenialStatus = ClaimDenialReason.Unavailable;
        character.IsActive = false;
        BlockProject();

        await CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.Discussed, statusChangedAt: StatusChangedAt));

        character.IsActive.ShouldBeTrue();
        claim.ClaimDenialStatus.ShouldBeNull();
    }

    [Fact]
    public async Task StatusDateInFuture_Throws()
    {
        var claim = CreateClaim(ClaimStatus.Discussed);

        _ = await Should.ThrowAsync<Exception>(
            () => CreateService().ApplyRestoredState(
                claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: DateTimeOffset.UtcNow.AddDays(10))));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Player_HasNoAccess()
    {
        var claim = CreateClaim(ClaimStatus.Discussed);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).ApplyRestoredState(
                claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt)));

        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
        SaveChangesCallCount.ShouldBe(0);
    }
}
