using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
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

    /// <summary>
    /// Восстановленные строки несут исторические даты, а дата последнего обновления проекта — максимум по
    /// заявкам, комментариям и т.п. Если её не поднять, после снятия блокировки проект сочтут давно
    /// не обновлявшимся и <c>ProjectPerformCloseJob</c> его закроет (требование в #5481, ADR023).
    /// </summary>
    [Fact]
    public async Task RestoreLostClaim_BumpsLastUpdateToNow_DespiteHistoricalDates()
    {
        var character = mock.CreateCharacter("Вася");
        BlockProject();
        var before = DateTime.UtcNow;

        await CreateService().RestoreLostClaim(LostClaim(
            character: character.GetId(),
            target: Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt)));

        var claim = LostClaimInMock;
        claim.CreateDate.ShouldBe(LostClaimCreatedAt.UtcDateTime);
        claim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(before);
        RestoreComments(claim).ShouldHaveSingleItem().LastEditTime.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public async Task ApplyRestoredState_BumpsLastUpdateToNow_DespiteHistoricalStatusDate()
    {
        var claim = CreateClaim(ClaimStatus.Discussed);
        claim.LastUpdateDateTime = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var before = DateTime.UtcNow;

        await CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));

        claim.MasterAcceptedDate.ShouldBe(StatusChangedAt.UtcDateTime);
        claim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(before);
        RestoreComments(claim).ShouldHaveSingleItem().LastEditTime.ShouldBeGreaterThanOrEqualTo(before);
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

    // --- Восстановление комментариев к заявке (#5492) ---

    private static readonly DateTimeOffset BackupAt = new(2026, 5, 20, 12, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset PlayerWroteAt = new(2026, 7, 1, 10, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset MasterAnsweredAt = new(2026, 7, 2, 10, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset MasterNotedAt = new(2026, 7, 3, 10, 0, 0, TimeSpan.FromHours(3));

    private static RestoredClaimComment RestoredComment(
        int? commentId, User author, bool byPlayer, bool visible, DateTimeOffset at, string text)
        => new(commentId, new UserIdentification(author.UserId), byPlayer, visible, ExtraAction: null, at, text);

    /// <summary>
    /// Заявка с отметками на момент бэкапа и восстановитель — отдельный мастер, чтобы его не спутать с
    /// авторами восстановленных комментариев.
    /// </summary>
    private (Claim Claim, User Restorer, User OtherMaster) CreateClaimForComments()
    {
        var restorer = mock.CreateMaster("Восстановитель");
        var otherMaster = mock.CreateMaster("Другой мастер");
        var claim = CreateClaim(ClaimStatus.Discussed);
        claim.LastPlayerCommentAt = BackupAt;
        claim.LastMasterCommentAt = BackupAt;
        claim.LastMasterCommentBy_Id = mock.Master.UserId;
        claim.LastVisibleMasterCommentAt = BackupAt;
        claim.LastVisibleMasterCommentBy_Id = mock.Master.UserId;
        claim.LastUpdateDateTime = BackupAt.UtcDateTime;
        return (claim, restorer, otherMaster);
    }

    private List<RestoredClaimComment> Correspondence(User otherMaster) =>
    [
        RestoredComment(500, mock.Player, byPlayer: true, visible: true, PlayerWroteAt, "Можно {другую} роль?"),
        RestoredComment(501, otherMaster, byPlayer: false, visible: true, MasterAnsweredAt, "Можно"),
        // До 2026-06-06 id в уведомлениях не было.
        RestoredComment(null, mock.Master, byPlayer: false, visible: false, MasterNotedAt, "Заметка для своих"),
    ];

    private static List<Comment> NonHistoryComments(Claim claim)
        => [.. claim.CommentDiscussion.Comments.Where(c => c.ExtraAction != CommentExtraAction.RestoredFromNotifications)];

    [Fact]
    public async Task RestoreComments_InsertsWithIds_WritesHistory_AndDenormalizes()
    {
        var (claim, restorer, otherMaster) = CreateClaimForComments();
        var before = DateTime.UtcNow;

        var result = await CreateService(restorer.UserId).RestoreClaimComments(claim.GetId(), Correspondence(otherMaster));

        result.ShouldBe(new RestoredCommentsResult(3, 0, []), new RestoredCommentsResultComparer());
        var restored = NonHistoryComments(claim);
        restored.Select(c => c.CommentId).Take(2).ShouldBe([500, 501]);
        restored[2].CommentId.ShouldBeGreaterThan(FakeCharacterAggregateWriteRepository.CommentIdentityCurrent);
        restored[0].CommentText.Text.Contents.ShouldBe("Можно {другую} роль?");
        restored[0].CreatedAt.ShouldBe(PlayerWroteAt.UtcDateTime);
        restored[1].AuthorUserId.ShouldBe(otherMaster.UserId);

        var history = RestoreComments(claim).ShouldHaveSingleItem();
        history.CommentText.Text.Contents.ShouldBe("Восстановлено комментариев из уведомлений: 3");
        history.IsVisibleToPlayer.ShouldBeFalse();
        history.AuthorUserId.ShouldBe(restorer.UserId);

        // Отметки — по восстановленным, а не «восстановитель, сейчас»: иначе сломается фильтр «без ответа».
        claim.LastPlayerCommentAt.ShouldBe(PlayerWroteAt);
        claim.LastMasterCommentAt.ShouldBe(MasterNotedAt);
        claim.LastMasterCommentBy_Id.ShouldBe(mock.Master.UserId);
        claim.LastVisibleMasterCommentAt.ShouldBe(MasterAnsweredAt);
        claim.LastVisibleMasterCommentBy_Id.ShouldBe(otherMaster.UserId);

        // Дата обновления заявки (а с ней и проекта) — сейчас, хотя комментарии исторические (ADR023).
        claim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(before);

        claimNotifications.Sent.ShouldBeEmpty();
        SentInOrder.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RestoreComments_LaterMarksBeforeOperation_AreKept()
    {
        var (claim, restorer, otherMaster) = CreateClaimForComments();
        var later = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        claim.LastPlayerCommentAt = later;
        claim.LastMasterCommentAt = later;
        claim.LastVisibleMasterCommentAt = later;

        _ = await CreateService(restorer.UserId).RestoreClaimComments(claim.GetId(), Correspondence(otherMaster));

        claim.LastPlayerCommentAt.ShouldBe(later);
        claim.LastMasterCommentAt.ShouldBe(later);
        claim.LastMasterCommentBy_Id.ShouldBe(mock.Master.UserId);
        claim.LastVisibleMasterCommentAt.ShouldBe(later);
        claim.LastVisibleMasterCommentBy_Id.ShouldBe(mock.Master.UserId);
    }

    [Fact]
    public async Task RestoreComments_Repeat_DoesNothing()
    {
        var (claim, restorer, otherMaster) = CreateClaimForComments();
        _ = await CreateService(restorer.UserId).RestoreClaimComments(claim.GetId(), Correspondence(otherMaster));
        var commentsCount = claim.CommentDiscussion.Comments.Count;
        var updatedAt = claim.LastUpdateDateTime;

        var result = await CreateService(restorer.UserId).RestoreClaimComments(claim.GetId(), Correspondence(otherMaster));

        result.ShouldBe(new RestoredCommentsResult(0, 3, []), new RestoredCommentsResultComparer());
        claim.CommentDiscussion.Comments.Count.ShouldBe(commentsCount);
        RestoreComments(claim).ShouldHaveSingleItem();
        claim.LastUpdateDateTime.ShouldBe(updatedAt);
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RestoreComments_AfterInterruptedRun_FixesMarksWithoutHistory()
    {
        // Прошлый вызов вставил комментарии и оборвался до операции над заявкой.
        var (claim, restorer, otherMaster) = CreateClaimForComments();
        foreach (var comment in Correspondence(otherMaster))
        {
            _ = await WriteRepository.InsertRestoredClaimComment(new RestoredClaimCommentRow(
                claim.GetId(), comment.CommentId, comment.Author, comment.IsCommentByPlayer, comment.IsVisibleToPlayer,
                comment.ExtraAction, comment.CreatedAt.UtcDateTime, comment.Text));
        }
        var before = DateTime.UtcNow;

        var result = await CreateService(restorer.UserId).RestoreClaimComments(claim.GetId(), Correspondence(otherMaster));

        result.ShouldBe(new RestoredCommentsResult(0, 3, []), new RestoredCommentsResultComparer());
        RestoreComments(claim).ShouldBeEmpty();
        claim.LastMasterCommentAt.ShouldBe(MasterNotedAt);
        claim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(before);
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RestoreComments_ConflictingIds_AreReported_AndSkipped()
    {
        var (claim, restorer, _) = CreateClaimForComments();
        var otherClaim = mock.CreateClaim(mock.CreateCharacter("Петя"), mock.Player);
        var foreign = mock.CreateComment(otherClaim, "Чужой комментарий");
        foreign.CommentId = 600;
        var notLost = FakeCharacterAggregateWriteRepository.CommentIdentityCurrent + 5;

        var result = await CreateService(restorer.UserId).RestoreClaimComments(claim.GetId(),
        [
            RestoredComment(600, mock.Player, byPlayer: true, visible: true, PlayerWroteAt, "Занятый id"),
            RestoredComment(notLost, mock.Player, byPlayer: true, visible: true, PlayerWroteAt, "Id выше счётчика"),
        ]);

        result.ShouldBe(new RestoredCommentsResult(0, 0, [600, notLost]), new RestoredCommentsResultComparer());
        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        claim.LastPlayerCommentAt.ShouldBe(BackupAt);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestoreComments_ActiveProject_Throws_AndNothingIsInserted()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateClaim(character, mock.Player);
        var otherMaster = mock.CreateMaster("Другой мастер");
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().RestoreClaimComments(claim.GetId(), Correspondence(otherMaster)));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestoreComments_Player_HasNoAccess()
    {
        var (claim, _, otherMaster) = CreateClaimForComments();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).RestoreClaimComments(claim.GetId(), Correspondence(otherMaster)));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestoreComments_DateInFuture_Throws_AndNothingIsInserted()
    {
        var (claim, _, otherMaster) = CreateClaimForComments();
        var comments = Correspondence(otherMaster);
        comments.Add(RestoredComment(502, mock.Player, byPlayer: true, visible: true, DateTimeOffset.UtcNow.AddDays(10), "Из будущего"));

        _ = await Should.ThrowAsync<CannotPerformOperationInFuture>(
            () => CreateService().RestoreClaimComments(claim.GetId(), comments));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
    }

    [Fact]
    public async Task RestoreComments_HiddenPlayerComment_Throws_AndNothingIsInserted()
    {
        var (claim, _, otherMaster) = CreateClaimForComments();
        var comments = Correspondence(otherMaster);
        comments.Add(RestoredComment(502, mock.Player, byPlayer: true, visible: false, PlayerWroteAt, "Скрытый от себя"));

        _ = await Should.ThrowAsync<ArgumentException>(() => CreateService().RestoreClaimComments(claim.GetId(), comments));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RestoreComments_EmptyTextWithoutAction_Throws_AndNothingIsInserted(string text)
    {
        var (claim, _, otherMaster) = CreateClaimForComments();
        var comments = Correspondence(otherMaster);
        comments.Add(RestoredComment(502, mock.Master, byPlayer: false, visible: true, MasterAnsweredAt, text));

        _ = await Should.ThrowAsync<ArgumentException>(() => CreateService().RestoreClaimComments(claim.GetId(), comments));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
    }

    [Fact]
    public async Task RestoreComments_EmptyTextWithAction_IsInserted()
    {
        var (claim, restorer, _) = CreateClaimForComments();

        var result = await CreateService(restorer.UserId).RestoreClaimComments(claim.GetId(),
            [RestoredComment(502, mock.Master, byPlayer: false, visible: true, MasterAnsweredAt, "") with { ExtraAction = CommentExtraAction.ChangeResponsible }]);

        result.Inserted.ShouldBe(1);
        NonHistoryComments(claim).ShouldHaveSingleItem().ExtraAction.ShouldBe(CommentExtraAction.ChangeResponsible);
    }

    [Fact]
    public async Task RestoreComments_DuplicateId_Throws_AndNothingIsInserted()
    {
        var (claim, _, otherMaster) = CreateClaimForComments();
        var comments = Correspondence(otherMaster);
        comments.Add(RestoredComment(500, mock.Player, byPlayer: true, visible: true, PlayerWroteAt, "Тот же id"));

        _ = await Should.ThrowAsync<ArgumentException>(() => CreateService().RestoreClaimComments(claim.GetId(), comments));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
    }

    [Fact]
    public async Task RestoreComments_ClaimFromOtherProjectOrMissing_Throws_AndNothingIsInserted()
    {
        var (claim, _, otherMaster) = CreateClaimForComments();

        _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => CreateService().RestoreClaimComments(new ClaimIdentification(ProjectId, 100500), Correspondence(otherMaster)));

        mock.Project.Claims.ShouldAllBe(c => c.CommentDiscussion.Comments.Count == 0);
        SaveChangesCallCount.ShouldBe(0);
        claim.CommentDiscussion.Comments.ShouldBeEmpty();
    }

    [Fact]
    public async Task RestoreComments_EmptyList_DoesNothing()
    {
        var (claim, restorer, _) = CreateClaimForComments();

        var result = await CreateService(restorer.UserId).RestoreClaimComments(claim.GetId(), []);

        result.ShouldBe(new RestoredCommentsResult(0, 0, []), new RestoredCommentsResultComparer());
        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        claim.LastUpdateDateTime.ShouldBe(BackupAt.UtcDateTime);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestoreComments_UnknownAuthor_Throws_AndNothingIsInserted()
    {
        var (claim, _, otherMaster) = CreateClaimForComments();
        var comments = Correspondence(otherMaster);
        comments.Add(new RestoredClaimComment(502, new UserIdentification(100500), false, true, null, PlayerWroteAt, "Кто это?"));

        _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => CreateService().RestoreClaimComments(claim.GetId(), comments));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
    }

    /// <summary>Записи сравнивают списки по ссылке — здесь нужно по содержимому.</summary>
    private sealed class RestoredCommentsResultComparer : IEqualityComparer<RestoredCommentsResult>
    {
        public bool Equals(RestoredCommentsResult? x, RestoredCommentsResult? y)
            => x is not null && y is not null
                && x.Inserted == y.Inserted && x.AlreadyExisted == y.AlreadyExisted
                && x.Conflicts.SequenceEqual(y.Conflicts);

        public int GetHashCode(RestoredCommentsResult obj) => HashCode.Combine(obj.Inserted, obj.AlreadyExisted);
    }
}
