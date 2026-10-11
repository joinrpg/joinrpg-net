using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
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

    private ClaimRestoreService CreateService(int? currentUserId = null)
        => new(CreatePropsService(currentUserId), unitOfWork, metadataRepository, CreateCurrentUser(currentUserId));

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

    // --- Восстановление потерянной заявки (её нет в восстановленной базе) ---

    private const int LostClaimId = 500;

    private static readonly DateTimeOffset LostClaimCreatedAt = new(2026, 6, 1, 10, 0, 0, TimeSpan.FromHours(3));

    private LostClaimToRestore LostClaim(
        CharacterIdentification? character = null,
        string? newCharacterName = "Потерянный",
        UserIdentification? responsibleMaster = null,
        RestoredClaimTarget? target = null)
        => new(
            new ClaimIdentification(ProjectId, LostClaimId),
            character,
            newCharacterName,
            new UserIdentification(mock.Player.UserId),
            responsibleMaster ?? new UserIdentification(mock.Master.UserId),
            LostClaimCreatedAt,
            target ?? Target());

    private Claim LostClaimInMock => mock.Project.Claims.Single(c => c.ClaimId == LostClaimId);

    [Fact]
    public async Task RestoreLostClaim_ApprovedOnCharacterWithOtherApprovedClaim_FailsBeforeWriting()
    {
        var character = mock.CreateCharacter("Занятый");
        _ = mock.CreateApprovedClaim(character, mock.CreateMaster("Другой игрок"));
        BlockProject();

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().RestoreLostClaim(LostClaim(
                character: character.GetId(), newCharacterName: null,
                target: Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt))));

        mock.Project.Claims.ShouldNotContain(c => c.ClaimId == LostClaimId);
    }

    [Fact]
    public async Task RestoreLostClaim_CharacterFromOtherProject_Throws()
    {
        BlockProject();
        var charactersCount = mock.Project.Characters.Count;

        _ = await Should.ThrowAsync<ArgumentException>(
            () => CreateService().RestoreLostClaim(LostClaim(character: new CharacterIdentification(new ProjectIdentification(9999), 1), newCharacterName: null)));

        mock.Project.Claims.ShouldNotContain(c => c.ClaimId == LostClaimId);
        mock.Project.Characters.Count.ShouldBe(charactersCount);
    }

    [Fact]
    public async Task RestoreLostClaim_TargetCharacter_Throws()
    {
        var other = mock.CreateCharacter("Другой");
        BlockProject();
        var charactersCount = mock.Project.Characters.Count;

        _ = await Should.ThrowAsync<ArgumentException>(
            () => CreateService().RestoreLostClaim(LostClaim(target: Target(character: other.GetId()))));

        mock.Project.Characters.Count.ShouldBe(charactersCount);
    }

    [Fact]
    public async Task RestoreLostClaim_CreatedInFuture_Throws()
    {
        BlockProject();
        var charactersCount = mock.Project.Characters.Count;
        var request = LostClaim() with { CreatedAt = DateTimeOffset.UtcNow.AddDays(10) };

        _ = await Should.ThrowAsync<CannotPerformOperationInFuture>(() => CreateService().RestoreLostClaim(request));

        mock.Project.Characters.Count.ShouldBe(charactersCount);
    }

    /// <summary>
    /// Поле-имя персонажа в проекте: тогда имя нового персонажа пишется его значением.
    /// </summary>
    private ProjectFieldInfo AddCharacterNameField()
    {
        var info = mock.AddField(f =>
        {
            f.FieldName = "Имя персонажа";
            f.FieldType = ProjectFieldType.String;
            f.FieldBoundTo = FieldBoundTo.Character;
            f.CanPlayerView = true;
        });
        mock.Project.Details.CharacterNameField = mock.Project.ProjectFields.Single(f => f.ProjectFieldId == info.Id.ProjectFieldId);
        mock.ReInitProjectInfo();
        return mock.ProjectInfo.CharacterNameField.ShouldNotBeNull();
    }

    [Fact]
    public async Task RestoreLostClaim_NewCharacter_CreatesHiddenPlayerCharacterInRootGroup()
    {
        BlockProject();
        var charactersBefore = mock.Project.Characters.ToList();

        await CreateService().RestoreLostClaim(LostClaim(newCharacterName: "Потерянный"));

        var created = mock.Project.Characters.Except(charactersBefore).ShouldHaveSingleItem();
        created.CharacterType.ShouldBe(CharacterType.Player);
        created.ParentCharacterGroupIds.ShouldBe([mock.Project.CharacterGroups.Single(g => g.IsRoot).CharacterGroupId]);
        created.IsPublic.ShouldBeFalse();
        created.CharacterName.ShouldBe("Потерянный");

        var claim = LostClaimInMock;
        claim.CharacterId.ShouldBe(created.CharacterId);
        claim.PlayerUserId.ShouldBe(mock.Player.UserId);
        claim.ResponsibleMasterUserId.ShouldBe(mock.Master.UserId);
        claim.CreateDate.ShouldBe(LostClaimCreatedAt.UtcDateTime);
        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByMaster);
    }

    [Fact]
    public async Task RestoreLostClaim_NewCharacter_NameGoesToNameField_WhenProjectHasIt()
    {
        var nameField = AddCharacterNameField();
        BlockProject();
        var charactersBefore = mock.Project.Characters.ToList();

        await CreateService().RestoreLostClaim(LostClaim(newCharacterName: "Потерянный"));

        var created = mock.Project.Characters.Except(charactersBefore).ShouldHaveSingleItem();
        created.CharacterName.ShouldBe("Потерянный");
        mock.GetCharacterInfo(created).CharacterFields.GetValue(mock.ProjectInfo.GetFieldById(nameField.Id)).ShouldBe("Потерянный");
    }

    [Fact]
    public async Task RestoreLostClaim_ExistingCharacter_IsUsed_AndNoCharacterCreated()
    {
        var character = mock.CreateCharacter("Вася");
        BlockProject();
        var charactersCount = mock.Project.Characters.Count;

        await CreateService().RestoreLostClaim(LostClaim(character: character.GetId(), newCharacterName: null));

        mock.Project.Characters.Count.ShouldBe(charactersCount);
        LostClaimInMock.CharacterId.ShouldBe(character.CharacterId);
    }

    [Fact]
    public async Task RestoreLostClaim_AppliesTargetStatus()
    {
        var character = mock.CreateCharacter("Вася");
        BlockProject();

        await CreateService().RestoreLostClaim(LostClaim(
            character: character.GetId(),
            target: Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt)));

        var claim = LostClaimInMock;
        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.MasterAcceptedDate.ShouldBe(StatusChangedAt.UtcDateTime);
        character.ApprovedClaimId.ShouldBe(LostClaimId);
    }

    [Fact]
    public async Task RestoreLostClaim_EmptyTarget_StillWritesSingleHiddenHistoryRecord_WithoutNotifications()
    {
        BlockProject();

        await CreateService().RestoreLostClaim(LostClaim());

        var claim = LostClaimInMock;
        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByMaster);
        var comment = claim.CommentDiscussion.Comments.ShouldHaveSingleItem();
        comment.ExtraAction.ShouldBe(CommentExtraAction.RestoredFromNotifications);
        comment.IsVisibleToPlayer.ShouldBeFalse();
        claimNotifications.Sent.ShouldBeEmpty();
        SentInOrder.ShouldBeEmpty();
    }

    [Fact]
    public async Task RestoreLostClaim_WithTarget_WritesSingleHiddenHistoryRecord_WithoutNotifications()
    {
        BlockProject();

        await CreateService().RestoreLostClaim(LostClaim(
            target: Target(status: ClaimStatus.Discussed, statusChangedAt: StatusChangedAt, manualFee: 700)));

        var comment = LostClaimInMock.CommentDiscussion.Comments.ShouldHaveSingleItem();
        comment.ExtraAction.ShouldBe(CommentExtraAction.RestoredFromNotifications);
        comment.IsVisibleToPlayer.ShouldBeFalse();
        claimNotifications.Sent.ShouldBeEmpty();
        SentInOrder.ShouldBeEmpty();
    }

    [Fact]
    public async Task RestoreLostClaim_ClaimAlreadyExists_Throws_AndNoCharacterCreated()
    {
        var existing = CreateClaim(ClaimStatus.Discussed);
        var charactersCount = mock.Project.Characters.Count;

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().RestoreLostClaim(LostClaim() with { ClaimId = existing.GetId() }));

        mock.Project.Characters.Count.ShouldBe(charactersCount);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestoreLostClaim_ActiveProject_Throws_AndNothingIsCreated()
    {
        var charactersCount = mock.Project.Characters.Count;

        _ = await Should.ThrowAsync<JoinValidationException>(() => CreateService().RestoreLostClaim(LostClaim()));

        mock.Project.Characters.Count.ShouldBe(charactersCount);
        mock.Project.Claims.ShouldNotContain(c => c.ClaimId == LostClaimId);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RestoreLostClaim_NoCharacterAndNoName_Throws(string? name)
    {
        BlockProject();
        var charactersCount = mock.Project.Characters.Count;

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().RestoreLostClaim(LostClaim(character: null, newCharacterName: name)));

        mock.Project.Characters.Count.ShouldBe(charactersCount);
        mock.Project.Claims.ShouldNotContain(c => c.ClaimId == LostClaimId);
    }

    [Fact]
    public async Task RestoreLostClaim_ResponsibleNotAMaster_Throws()
    {
        BlockProject();
        var charactersCount = mock.Project.Characters.Count;

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().RestoreLostClaim(LostClaim(responsibleMaster: new UserIdentification(mock.Player.UserId))));

        mock.Project.Characters.Count.ShouldBe(charactersCount);
        mock.Project.Claims.ShouldNotContain(c => c.ClaimId == LostClaimId);
    }

    [Fact]
    public async Task RestoreLostClaim_Player_HasNoAccess()
    {
        BlockProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).RestoreLostClaim(LostClaim()));

        mock.Project.Claims.ShouldNotContain(c => c.ClaimId == LostClaimId);
    }
}
