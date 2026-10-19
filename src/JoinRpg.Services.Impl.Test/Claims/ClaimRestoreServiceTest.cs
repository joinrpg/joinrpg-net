using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
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

    // --- Принятая заявка на шаблон (#5645): как при одобрении, получает своего персонажа из шаблона ---

    private Claim CreateClaimOnSlot(Character slot, ClaimStatus status = ClaimStatus.Discussed)
    {
        var claim = mock.CreateClaim(slot, mock.Player);
        claim.ClaimStatus = status;
        return claim;
    }

    /// <summary>
    /// Что сделало бы сохранение EF6: персонаж из шаблона получает id, а заявка переезжает в его
    /// коллекцию заявок. Без этого следующая операция над заявкой или шаблоном увидит мок до сохранения.
    /// </summary>
    private void SimulateSaveOfCharacterFromSlot(Claim claim, Character slot)
    {
        var created = claim.Character;
        created.CharacterId = mock.Project.Characters.Max(c => c.CharacterId) + 1;
        claim.CharacterId = created.CharacterId;
        _ = slot.Claims.Remove(claim);
        _ = created.Claims.Add(claim);
    }

    [Fact]
    public async Task Approve_OnSlot_CreatesOwnCharacterFromSlot()
    {
        var slot = mock.CreateSlot("Слот", slotLimit: 3);
        var plot = mock.CreatePlotElement(slot);
        var claim = CreateClaimOnSlot(slot);
        BlockProject();

        await CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        var created = claim.Character;
        created.ShouldNotBe(slot);
        created.AutoCreated.ShouldBeTrue();
        created.OriginalCharacterSlot.ShouldBe(slot);
        created.CharacterType.ShouldBe(CharacterType.Player);
        created.ApprovedClaim.ShouldBe(claim);
        mock.Project.Characters.ShouldContain(created);
        plot.TargetCharacters.ShouldContain(created);

        slot.CharacterType.ShouldBe(CharacterType.Slot);
        slot.CharacterSlotLimit.ShouldBe(2);
        slot.ApprovedClaimId.ShouldBeNull();

        RestoreComments(claim).ShouldHaveSingleItem().CommentText.Text.Contents.ShouldNotBeNull().ShouldContain("персонаж создан из шаблона «Слот»");
        claimNotifications.Sent.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task CheckedIn_OnSlot_CreatesOwnCharacterInGame()
    {
        var slot = mock.CreateSlot("Слот");
        var claim = CreateClaimOnSlot(slot);
        BlockProject();

        await CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.CheckedIn, statusChangedAt: StatusChangedAt));

        var created = claim.Character;
        created.OriginalCharacterSlot.ShouldBe(slot);
        created.ApprovedClaimId.ShouldBe(claim.ClaimId);
        created.InGame.ShouldBeTrue();
        slot.CharacterSlotLimit.ShouldBeNull();
    }

    [Fact]
    public async Task Approve_TwoClaimsOnSlot_EachGetsOwnCharacter()
    {
        var slot = mock.CreateSlot("Слот", slotLimit: 5);
        var first = CreateClaimOnSlot(slot);
        var second = mock.CreateClaim(slot, mock.CreateMaster("Другой игрок"));
        second.ClaimStatus = ClaimStatus.Discussed;
        BlockProject();

        await CreateService().ApplyRestoredState(first.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));
        SimulateSaveOfCharacterFromSlot(first, slot);
        await CreateService().ApplyRestoredState(second.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));

        first.Character.OriginalCharacterSlot.ShouldBe(slot);
        second.Character.OriginalCharacterSlot.ShouldBe(slot);
        second.Character.ShouldNotBe(first.Character);
        first.Character.ApprovedClaimId.ShouldBe(first.ClaimId);
        second.Character.ApprovedClaimId.ShouldBe(second.ClaimId);
        slot.CharacterSlotLimit.ShouldBe(3);
    }

    [Fact]
    public async Task Approve_OnExhaustedSlot_DoesNotThrow_AndLimitStaysZero()
    {
        var slot = mock.CreateSlot("Слот", slotLimit: 0);
        var claim = CreateClaimOnSlot(slot);
        BlockProject();

        await CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.Character.OriginalCharacterSlot.ShouldBe(slot);
        slot.CharacterSlotLimit.ShouldBe(0);
    }

    [Fact]
    public async Task Discussed_OnSlot_StaysOnSlot()
    {
        var slot = mock.CreateSlot("Слот", slotLimit: 3);
        var claim = CreateClaimOnSlot(slot, ClaimStatus.AddedByUser);
        BlockProject();
        var charactersCount = mock.Project.Characters.Count;

        await CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.Discussed, statusChangedAt: StatusChangedAt));

        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
        claim.Character.ShouldBe(slot);
        slot.CharacterSlotLimit.ShouldBe(3);
        mock.Project.Characters.Count.ShouldBe(charactersCount);
    }

    [Fact]
    public async Task Approve_OnSlot_Repeat_IsNoOp()
    {
        var slot = mock.CreateSlot("Слот", slotLimit: 3);
        var claim = CreateClaimOnSlot(slot);
        BlockProject();
        var target = Target(character: slot.GetId(), status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt);

        await CreateService().ApplyRestoredState(claim.GetId(), target);
        SimulateSaveOfCharacterFromSlot(claim, slot);
        var created = claim.Character;
        var charactersCount = mock.Project.Characters.Count;

        await CreateService().ApplyRestoredState(claim.GetId(), target);

        claim.Character.ShouldBe(created);
        mock.Project.Characters.Count.ShouldBe(charactersCount);
        slot.CharacterSlotLimit.ShouldBe(2);
        RestoreComments(claim).ShouldHaveSingleItem();
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task MoveToSlotAndApprove_CreatesOwnCharacterFromSlot()
    {
        var oldCharacter = mock.CreateCharacter("Вася");
        var slot = mock.CreateSlot("Слот", slotLimit: 3);
        var claim = mock.CreateApprovedClaim(oldCharacter, mock.Player);
        BlockProject();

        await CreateService().ApplyRestoredState(claim.GetId(), Target(character: slot.GetId()));

        oldCharacter.ApprovedClaimId.ShouldBeNull();
        claim.Character.OriginalCharacterSlot.ShouldBe(slot);
        claim.Character.ApprovedClaimId.ShouldBe(claim.ClaimId);
        slot.ApprovedClaimId.ShouldBeNull();
        slot.CharacterSlotLimit.ShouldBe(2);
    }

    [Fact]
    public async Task MoveFromSlotToOtherSlotAndApprove_CreatesCharacterFromTargetSlot()
    {
        var firstSlot = mock.CreateSlot("Первый", slotLimit: 3);
        var secondSlot = mock.CreateSlot("Второй", slotLimit: 3);
        var claim = CreateClaimOnSlot(firstSlot);
        BlockProject();

        await CreateService().ApplyRestoredState(
            claim.GetId(), Target(character: secondSlot.GetId(), status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));

        claim.Character.OriginalCharacterSlot.ShouldBe(secondSlot);
        firstSlot.CharacterSlotLimit.ShouldBe(3);
        secondSlot.CharacterSlotLimit.ShouldBe(2);
    }

    /// <summary>Заявка, уже принятая на персонажа, созданного из шаблона.</summary>
    private (Claim Claim, Character Created) CreateApprovedClaimOnCharacterFromSlot(Character slot)
    {
        var created = mock.CreateCharacter("Из шаблона");
        created.AutoCreated = true;
        created.OriginalCharacterSlot = slot;
        var claim = mock.CreateApprovedClaim(created, mock.Player);
        return (claim, created);
    }

    [Fact]
    public async Task NotApprovedClaim_OnCharacterFromSlot_WithSlotAsTarget_MovesToSlot()
    {
        var slot = mock.CreateSlot("Слот", slotLimit: 3);
        var (claim, created) = CreateApprovedClaimOnCharacterFromSlot(slot);
        BlockProject();

        await CreateService().ApplyRestoredState(
            claim.GetId(), Target(character: slot.GetId(), status: ClaimStatus.Discussed, statusChangedAt: StatusChangedAt));

        claim.Character.ShouldBe(slot);
        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
        created.ApprovedClaimId.ShouldBeNull();
        slot.CharacterSlotLimit.ShouldBe(3);
    }

    [Fact]
    public async Task ApprovedClaim_OnCharacterFromSlot_MovesToOtherCharacter()
    {
        var slot = mock.CreateSlot("Слот", slotLimit: 3);
        var (claim, created) = CreateApprovedClaimOnCharacterFromSlot(slot);
        var other = mock.CreateCharacter("Петя");
        BlockProject();

        await CreateService().ApplyRestoredState(claim.GetId(), Target(character: other.GetId()));

        claim.Character.ShouldBe(other);
        other.ApprovedClaimId.ShouldBe(claim.ClaimId);
        created.ApprovedClaimId.ShouldBeNull();
        slot.CharacterSlotLimit.ShouldBe(3);
    }

    [Fact]
    public async Task Approve_OnSlot_WithoutNameField_NamesCharacterAfterPlayer()
    {
        var slot = mock.CreateSlot("Слот");
        var claim = CreateClaimOnSlot(slot);
        BlockProject();

        await CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));

        claim.Character.CharacterName.ShouldBe(mock.Player.GetDisplayName());
        slot.CharacterName.ShouldBe("Слот");
    }

    [Fact]
    public async Task Approve_OnSlot_WithNameField_NamesCharacterFromClaimFields()
    {
        var nameField = AddCharacterNameField();
        var slot = mock.CreateSlot("Слот");
        var claim = CreateClaimOnSlot(slot);
        MockedProject.AssignFieldValues(claim, new FieldWithValue(nameField, "Имя из заявки"));
        BlockProject();

        await CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));

        var created = claim.Character;
        created.CharacterName.ShouldBe("Имя из заявки");
        SimulateSaveOfCharacterFromSlot(claim, slot);
        mock.GetCharacterInfo(created).CharacterFields.GetValue(mock.ProjectInfo.GetFieldById(nameField.Id)).ShouldBe("Имя из заявки");
    }

    /// <summary>
    /// Текущее поведение, общее с одобрением (ApproveCore): пустое поле-имя даёт имя «CHAR» + id, а id у
    /// персонажа из шаблона до сохранения ещё -1. Фиксируется, а не чинится здесь.
    /// </summary>
    [Fact]
    public async Task Approve_OnSlot_WithEmptyNameField_NamesCharacterWithUnsavedId()
    {
        _ = AddCharacterNameField();
        var slot = mock.CreateSlot("Слот");
        var claim = CreateClaimOnSlot(slot);
        BlockProject();

        await CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));

        claim.Character.CharacterName.ShouldBe("CHAR-1");
    }

    [Fact]
    public async Task Approve_OnSlot_WithEmptyRequiredField_DoesNotThrow()
    {
        _ = mock.AddField(f =>
        {
            f.FieldName = "Обязательное";
            f.FieldType = ProjectFieldType.String;
            f.FieldBoundTo = FieldBoundTo.Character;
            f.MandatoryStatus = MandatoryStatus.Required;
        });
        var slot = mock.CreateSlot("Слот");
        var claim = CreateClaimOnSlot(slot);
        BlockProject();

        await CreateService().ApplyRestoredState(claim.GetId(), Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt));

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.Character.OriginalCharacterSlot.ShouldBe(slot);
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
    public async Task RestoreLostClaim_NewSlot_InDiscussion_StaysOnHiddenUnlimitedSlot()
    {
        BlockProject();
        var charactersBefore = mock.Project.Characters.ToList();

        var returned = await CreateService().RestoreLostClaim(
            LostClaim(newCharacterName: "Шаблон", target: Target(status: ClaimStatus.Discussed, statusChangedAt: StatusChangedAt))
            with
            { NewCharacterIsSlot = true });

        var slot = mock.Project.Characters.Except(charactersBefore).ShouldHaveSingleItem();
        slot.CharacterType.ShouldBe(CharacterType.Slot);
        slot.CharacterSlotLimit.ShouldBeNull();
        slot.IsPublic.ShouldBeFalse();
        slot.CharacterName.ShouldBe("Шаблон");
        returned.ShouldBe(slot.GetId());
        LostClaimInMock.CharacterId.ShouldBe(slot.CharacterId);
    }

    [Fact]
    public async Task RestoreLostClaim_NewSlot_Approved_CreatesOwnCharacterFromSlot()
    {
        BlockProject();
        var charactersBefore = mock.Project.Characters.ToList();

        var returned = await CreateService().RestoreLostClaim(
            LostClaim(newCharacterName: "Шаблон", target: Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt))
            with
            { NewCharacterIsSlot = true });

        var slot = mock.Project.Characters.Except(charactersBefore).Single(c => c.CharacterType == CharacterType.Slot);
        returned.ShouldBe(slot.GetId());
        var claim = LostClaimInMock;
        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.Character.OriginalCharacterSlot.ShouldBe(slot);
        claim.Character.ApprovedClaimId.ShouldBe(LostClaimId);
        slot.ApprovedClaimId.ShouldBeNull();
        slot.CharacterSlotLimit.ShouldBeNull();
    }

    [Fact]
    public async Task RestoreLostClaim_ExistingSlot_Approved_CreatesOwnCharacterFromSlot()
    {
        var slot = mock.CreateSlot("Слот", slotLimit: 1);
        BlockProject();

        _ = await CreateService().RestoreLostClaim(LostClaim(
            character: slot.GetId(), newCharacterName: null,
            target: Target(status: ClaimStatus.Approved, statusChangedAt: StatusChangedAt)));

        LostClaimInMock.Character.OriginalCharacterSlot.ShouldBe(slot);
        slot.CharacterSlotLimit.ShouldBe(0);
    }

    [Fact]
    public async Task RestoreLostClaim_SlotFlagWithExistingCharacter_Throws()
    {
        var character = mock.CreateCharacter("Вася");
        BlockProject();

        _ = await Should.ThrowAsync<ArgumentException>(
            () => CreateService().RestoreLostClaim(LostClaim(character: character.GetId(), newCharacterName: null) with { NewCharacterIsSlot = true }));

        mock.Project.Claims.ShouldNotContain(c => c.ClaimId == LostClaimId);
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

    // --- Восстановление ручных финансовых операций (#5561) ---

    // 22:30 UTC — в Москве (часовой пояс проекта) уже следующий день.
    private static readonly DateTimeOffset MasterAcceptedAt = new(2026, 7, 1, 22, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PlayerPaidAt = new(2026, 7, 5, 10, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset ModeratedAt = new(2026, 7, 6, 10, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset RefundedAt = new(2026, 7, 10, 10, 0, 0, TimeSpan.FromHours(3));

    /// <summary>
    /// Заявка с отметками на момент бэкапа, наличный тип оплаты мастера и восстановитель — отдельный
    /// мастер, чтобы его не спутать с авторами восстановленного.
    /// </summary>
    private (Claim Claim, User Restorer, User OtherMaster, PaymentType Cash) CreateClaimForFinance()
    {
        var cash = mock.CreateCashPaymentType(mock.Master);
        var (claim, restorer, otherMaster) = CreateClaimForComments();
        return (claim, restorer, otherMaster, cash);
    }

    private PaymentTypeIdentification PaymentTypeId(PaymentType paymentType) => new(ProjectId, paymentType.PaymentTypeId);

    private RestoredFinanceOperation Operation(
        int? id, User author, bool byPlayer, PaymentType paymentType, int money, FinanceOperationState state,
        DateTimeOffset at, DateTimeOffset? changedAt = null, string text = "Оплата")
        => new(id, new UserIdentification(author.UserId), byPlayer,
            money > 0 ? FinanceOperationType.Submit : FinanceOperationType.Refund,
            money, PaymentTypeId(paymentType), state, at, changedAt ?? at, text);

    private static RestoredFinanceModeration Moderation(
        int? id, User author, bool approved, DateTimeOffset at, int? parentOperationId = null, int? parentRestoredIndex = null)
        => new(id, new UserIdentification(author.UserId), approved, at, approved ? "Пришло" : "Не пришло", parentOperationId, parentRestoredIndex);

    /// <summary>Операция, которая уже есть в базе: комментарий с финансовой операцией.</summary>
    private FinanceOperation AddExistingOperation(
        Claim claim, int id, FinanceOperationType type, int money, FinanceOperationState state, PaymentType? paymentType)
    {
        var comment = mock.CreateComment(claim, "Оплатил", extraAction: CommentExtraAction.PaidFee);
        comment.CommentId = id;
        var operation = new FinanceOperation
        {
            CommentId = id,
            Comment = comment,
            ProjectId = claim.ProjectId,
            ClaimId = claim.ClaimId,
            Claim = claim,
            MoneyAmount = money,
            PaymentTypeId = paymentType?.PaymentTypeId,
            PaymentType = paymentType,
            OperationType = type,
            State = state,
            Created = BackupAt.UtcDateTime,
            Changed = BackupAt.UtcDateTime,
            OperationDate = BackupAt.UtcDateTime.Date,
        };
        comment.Finance = operation;
        claim.FinanceOperations.Add(operation);
        return operation;
    }

    private void AddFeeSetting(DateTime startDate, int fee)
    {
        mock.Project.ProjectFeeSettings.Add(new ProjectFeeSetting { StartDate = startDate, Fee = fee });
        mock.ReInitProjectInfo();
    }

    [Fact]
    public async Task RestoreFinance_InsertsWithIds_LikeAcceptFee_WritesHistory_AndDenormalizes()
    {
        var (claim, restorer, otherMaster, cash) = CreateClaimForFinance();
        var before = DateTime.UtcNow;

        var result = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(),
        [
            Operation(600, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt, text: "Принял {наличными}"),
            // До 2026-06-06 id в уведомлениях не было; игрок сообщил об оплате, мастер подтвердил.
            Operation(null, mock.Player, byPlayer: true, cash, 500, FinanceOperationState.Approved, PlayerPaidAt, ModeratedAt),
            Operation(602, mock.Master, byPlayer: false, cash, -200, FinanceOperationState.Approved, RefundedAt),
        ],
        [Moderation(601, otherMaster, approved: true, ModeratedAt, parentRestoredIndex: 1)]);

        result.Operations.ShouldAllBe(e => e.Status == RestoredFinanceEntryStatus.Inserted);
        result.Operations[0].Id.ShouldBe(600);
        result.Operations[1].Id!.Value.ShouldBeGreaterThan(FakeCharacterAggregateWriteRepository.CommentIdentityCurrent);
        result.Operations[2].Id.ShouldBe(602);
        result.Moderations.ShouldBe([new RestoredFinanceEntry(RestoredFinanceEntryStatus.Inserted, 601)]);
        result.Inserted.ShouldBe(4);

        var accepted = claim.FinanceOperations.Single(f => f.CommentId == 600);
        accepted.OperationType.ShouldBe(FinanceOperationType.Submit);
        accepted.MoneyAmount.ShouldBe(1000);
        accepted.State.ShouldBe(FinanceOperationState.Approved);
        accepted.PaymentTypeId.ShouldBe(cash.PaymentTypeId);
        accepted.Created.ShouldBe(MasterAcceptedAt.UtcDateTime);
        accepted.Changed.ShouldBe(MasterAcceptedAt.UtcDateTime);
        // Как у формы приёма взноса: полночь дня без часового пояса; день — в часовом поясе проекта.
        accepted.OperationDate.ShouldBe(new DateTime(2026, 7, 2));
        accepted.OperationDate.Kind.ShouldBe(DateTimeKind.Unspecified);
        accepted.Comment.ExtraAction.ShouldBe(CommentExtraAction.PaidFee);
        accepted.Comment.IsVisibleToPlayer.ShouldBeTrue();
        accepted.Comment.IsCommentByPlayer.ShouldBeFalse();
        accepted.Comment.AuthorUserId.ShouldBe(mock.Master.UserId);
        accepted.Comment.CommentText.Text.Contents.ShouldBe("Принял {наличными}");

        var reported = claim.FinanceOperations.Single(f => f.CommentId == result.Operations[1].Id);
        reported.Changed.ShouldBe(ModeratedAt.UtcDateTime);
        reported.Comment.IsCommentByPlayer.ShouldBeTrue();
        reported.Comment.AuthorUserId.ShouldBe(mock.Player.UserId);

        var refund = claim.FinanceOperations.Single(f => f.CommentId == 602);
        refund.OperationType.ShouldBe(FinanceOperationType.Refund);
        refund.MoneyAmount.ShouldBe(-200);
        refund.Comment.ExtraAction.ShouldBe(CommentExtraAction.RefundFee);

        var moderation = claim.CommentDiscussion.Comments.Single(c => c.CommentId == 601);
        moderation.ParentCommentId.ShouldBe(reported.CommentId);
        moderation.ExtraAction.ShouldBe(CommentExtraAction.ApproveFinance);
        moderation.IsVisibleToPlayer.ShouldBeTrue();
        moderation.IsCommentByPlayer.ShouldBeFalse();
        moderation.AuthorUserId.ShouldBe(otherMaster.UserId);
        moderation.CreatedAt.ShouldBe(ModeratedAt.UtcDateTime);

        var history = RestoreComments(claim).ShouldHaveSingleItem();
        history.CommentText.Text.Contents.ShouldBe("Восстановлено финансовых операций из уведомлений: 3, решений по ним: 1");
        history.IsVisibleToPlayer.ShouldBeFalse();
        history.AuthorUserId.ShouldBe(restorer.UserId);

        // Отметки — по восстановленным (все видимы игроку), а не «восстановитель, сейчас».
        claim.LastPlayerCommentAt.ShouldBe(PlayerPaidAt);
        claim.LastMasterCommentAt.ShouldBe(RefundedAt);
        claim.LastMasterCommentBy_Id.ShouldBe(mock.Master.UserId);
        claim.LastVisibleMasterCommentAt.ShouldBe(RefundedAt);
        claim.LastVisibleMasterCommentBy_Id.ShouldBe(mock.Master.UserId);

        // Дата обновления заявки (а с ней и проекта) — сейчас, хотя операции исторические (ADR023).
        claim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(before);

        claimNotifications.Sent.ShouldBeEmpty();
        SentInOrder.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RestoreFinance_Repeat_DoesNothing()
    {
        var (claim, restorer, otherMaster, cash) = CreateClaimForFinance();
        List<RestoredFinanceOperation> operations =
        [
            Operation(600, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt),
            Operation(null, mock.Player, byPlayer: true, cash, 500, FinanceOperationState.Declined, PlayerPaidAt, ModeratedAt),
        ];
        List<RestoredFinanceModeration> moderations = [Moderation(null, otherMaster, approved: false, ModeratedAt, parentRestoredIndex: 1)];
        var first = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), operations, moderations);
        var commentsCount = claim.CommentDiscussion.Comments.Count;
        var updatedAt = claim.LastUpdateDateTime;

        var repeat = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), operations, moderations);

        repeat.Operations.ShouldAllBe(e => e.Status == RestoredFinanceEntryStatus.AlreadyExisted);
        // Операция без id узнана и отдана под тем же id, что получила в первый раз.
        repeat.Operations.Select(e => e.Id).ShouldBe(first.Operations.Select(e => e.Id));
        repeat.Moderations.ShouldBe([new RestoredFinanceEntry(RestoredFinanceEntryStatus.AlreadyExisted, null)]);
        repeat.Inserted.ShouldBe(0);
        claim.CommentDiscussion.Comments.Count.ShouldBe(commentsCount);
        claim.FinanceOperations.Count.ShouldBe(2);
        RestoreComments(claim).ShouldHaveSingleItem();
        claim.LastUpdateDateTime.ShouldBe(updatedAt);
        SaveChangesCallCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(true, FinanceOperationState.Approved, 1000)]
    [InlineData(false, FinanceOperationState.Declined, null)]
    public async Task RestoreFinance_ModerationOfExistingProposedOperation_SetsStateAndFixesFee(
        bool approved, FinanceOperationState expectedState, int? expectedFee)
    {
        AddFeeSetting(new DateTime(2026, 1, 1), 1000);
        var (claim, restorer, otherMaster, cash) = CreateClaimForFinance();
        var existing = AddExistingOperation(claim, 700, FinanceOperationType.Submit, 1000, FinanceOperationState.Proposed, cash);
        var before = DateTime.UtcNow;

        var result = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), [],
            [Moderation(null, otherMaster, approved, ModeratedAt, parentOperationId: 700)]);

        result.Moderations.ShouldBe([new RestoredFinanceEntry(RestoredFinanceEntryStatus.Inserted, null)]);
        existing.State.ShouldBe(expectedState);
        existing.Changed.ShouldBe(ModeratedAt.UtcDateTime);
        claim.CurrentFee.ShouldBe(expectedFee);
        var moderation = NonHistoryComments(claim).Single(c => c.ParentCommentId == 700);
        moderation.ExtraAction.ShouldBe(approved ? CommentExtraAction.ApproveFinance : CommentExtraAction.RejectFinance);
        RestoreComments(claim).ShouldHaveSingleItem().CommentText.Text.Contents
            .ShouldBe("Восстановлено финансовых операций из уведомлений: 0, решений по ним: 1");
        claim.LastVisibleMasterCommentAt.ShouldBe(ModeratedAt);
        claim.LastVisibleMasterCommentBy_Id.ShouldBe(otherMaster.UserId);
        claim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(before);
        claimNotifications.Sent.ShouldBeEmpty();

        // Повтор: решение узнано, состояние уже итоговое — ничего не меняется.
        var repeat = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), [],
            [Moderation(null, otherMaster, approved, ModeratedAt, parentOperationId: 700)]);
        repeat.Moderations.ShouldBe([new RestoredFinanceEntry(RestoredFinanceEntryStatus.AlreadyExisted, null)]);
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RestoreFinance_ModerationWithoutParent_IsInsertedAsPlainComment_AndChangesNoState()
    {
        // Решение, которое не удалось связать с операцией: рядом ждёт решения платёж, но его не трогаем.
        var (claim, restorer, otherMaster, cash) = CreateClaimForFinance();
        var waiting = AddExistingOperation(claim, 700, FinanceOperationType.Submit, 1000, FinanceOperationState.Proposed, cash);
        RestoredFinanceModeration[] moderations =
        [
            Moderation(701, otherMaster, approved: true, ModeratedAt),
            Moderation(null, otherMaster, approved: false, ModeratedAt.AddHours(1)),
        ];

        var result = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), [], moderations);

        result.Moderations.ShouldBe(
        [
            new RestoredFinanceEntry(RestoredFinanceEntryStatus.Inserted, 701),
            new RestoredFinanceEntry(RestoredFinanceEntryStatus.Inserted, null),
        ]);
        var approve = claim.CommentDiscussion.Comments.Single(c => c.CommentId == 701);
        approve.ParentCommentId.ShouldBeNull();
        approve.ExtraAction.ShouldBe(CommentExtraAction.ApproveFinance);
        approve.IsVisibleToPlayer.ShouldBeTrue();
        var reject = NonHistoryComments(claim).Single(c => c.ExtraAction == CommentExtraAction.RejectFinance);
        reject.ParentCommentId.ShouldBeNull();
        waiting.State.ShouldBe(FinanceOperationState.Proposed);
        waiting.Changed.ShouldBe(BackupAt.UtcDateTime);
        RestoreComments(claim).ShouldHaveSingleItem().CommentText.Text.Contents
            .ShouldBe("Восстановлено финансовых операций из уведомлений: 0, решений по ним: 2");
        claim.LastVisibleMasterCommentAt.ShouldBe(ModeratedAt.AddHours(1));
        SaveChangesCallCount.ShouldBe(1);

        var repeat = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), [], moderations);

        repeat.Moderations.ShouldAllBe(e => e.Status == RestoredFinanceEntryStatus.AlreadyExisted);
        waiting.State.ShouldBe(FinanceOperationState.Proposed);
        RestoreComments(claim).ShouldHaveSingleItem();
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RestoreFinance_ModerationOfPreferentialFeeRequest_OnlyAddsComment()
    {
        var (claim, restorer, otherMaster, _) = CreateClaimForFinance();
        var request = AddExistingOperation(claim, 700, FinanceOperationType.PreferentialFeeRequest, 0, FinanceOperationState.Proposed, paymentType: null);

        var result = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), [],
            [Moderation(701, otherMaster, approved: true, ModeratedAt, parentOperationId: 700)]);

        result.Moderations.ShouldBe([new RestoredFinanceEntry(RestoredFinanceEntryStatus.Inserted, 701)]);
        request.State.ShouldBe(FinanceOperationState.Proposed);
        claim.PreferentialFeeUser.ShouldBeFalse();
        claim.CommentDiscussion.Comments.Single(c => c.CommentId == 701).ParentCommentId.ShouldBe(700);
    }

    [Fact]
    public async Task RestoreFinance_ModerationOfRestoredOperation_WithOtherState_Throws_AndNothingIsInserted()
    {
        var (claim, restorer, otherMaster, cash) = CreateClaimForFinance();

        // Решение «подтверждена» при итоговом состоянии «ждёт решения» — вход противоречив.
        await ShouldRejectFinance<ArgumentException>(claim, restorer.UserId,
            [Operation(600, mock.Player, byPlayer: true, cash, 500, FinanceOperationState.Proposed, PlayerPaidAt)],
            [Moderation(601, otherMaster, approved: true, ModeratedAt, parentOperationId: 600)]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RestoreFinance_RepeatWithDecision_AfterRunWithoutIt_ApprovesOperation(bool withId)
    {
        // Первый прогон: игрок сообщил об оплате, решения ещё не нашли. Второй: решение нашлось.
        AddFeeSetting(new DateTime(2026, 1, 1), 1000);
        var (claim, restorer, otherMaster, cash) = CreateClaimForFinance();
        var proposed = Operation(withId ? 600 : null, mock.Player, byPlayer: true, cash, 1000, FinanceOperationState.Proposed, PlayerPaidAt);
        var first = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), [proposed], []);
        var operationId = first.Operations.ShouldHaveSingleItem().Id!.Value;
        claim.CurrentFee.ShouldBeNull();

        var repeat = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(),
            [proposed with { State = FinanceOperationState.Approved, ChangedAt = ModeratedAt }],
            [withId
                ? Moderation(601, otherMaster, approved: true, ModeratedAt, parentOperationId: 600)
                : Moderation(null, otherMaster, approved: true, ModeratedAt, parentRestoredIndex: 0)]);

        repeat.Operations.ShouldBe([new RestoredFinanceEntry(RestoredFinanceEntryStatus.AlreadyExisted, operationId)]);
        repeat.Moderations.ShouldHaveSingleItem().Status.ShouldBe(RestoredFinanceEntryStatus.Inserted);
        var operation = claim.FinanceOperations.ShouldHaveSingleItem();
        operation.State.ShouldBe(FinanceOperationState.Approved);
        operation.Changed.ShouldBe(ModeratedAt.UtcDateTime);
        claim.CurrentFee.ShouldBe(1000);
    }

    [Fact]
    public async Task RestoreFinance_ModerationOfExistingOnlineOperation_Throws_AndNothingIsInserted()
    {
        var (claim, restorer, otherMaster, cash) = CreateClaimForFinance();
        var online = AddExistingOperation(claim, 700, FinanceOperationType.Online, 1000, FinanceOperationState.Proposed, cash);
        var commentsBefore = claim.CommentDiscussion.Comments.Count;

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), [],
                [Moderation(null, otherMaster, approved: true, ModeratedAt, parentOperationId: 700)]));

        online.State.ShouldBe(FinanceOperationState.Proposed);
        claim.CommentDiscussion.Comments.Count.ShouldBe(commentsBefore);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestoreFinance_TwoSameOperationsWithoutId_AreOneRow()
    {
        var (claim, restorer, _, cash) = CreateClaimForFinance();
        var operation = Operation(null, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt);

        var result = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), [operation, operation], []);

        result.Operations[0].Status.ShouldBe(RestoredFinanceEntryStatus.Inserted);
        result.Operations[1].ShouldBe(new RestoredFinanceEntry(RestoredFinanceEntryStatus.AlreadyExisted, result.Operations[0].Id));
        claim.FinanceOperations.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task RestoreFinance_FixesFee_ByPaymentsInDateOrder()
    {
        // Взнос подорожал до того, как оплачен полностью: закрыт он только сентябрьским платежом, по новой
        // цене. Без порядка дат июльский платёж «увидел» бы всю сумму и зафиксировал старую цену.
        AddFeeSetting(new DateTime(2026, 1, 1), 1000);
        AddFeeSetting(new DateTime(2026, 8, 1), 2000);
        var (claim, restorer, _, cash) = CreateClaimForFinance();

        _ = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(),
        [
            Operation(null, mock.Master, byPlayer: false, cash, 1500, FinanceOperationState.Approved, new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero)),
            Operation(null, mock.Master, byPlayer: false, cash, 500, FinanceOperationState.Approved, new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero)),
        ], []);

        claim.CurrentFee.ShouldBe(2000);
    }

    [Fact]
    public async Task RestoreFinance_ProposedPayment_DoesNotFixFee()
    {
        AddFeeSetting(new DateTime(2026, 1, 1), 1000);
        var (claim, restorer, _, cash) = CreateClaimForFinance();

        _ = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(),
            [Operation(600, mock.Player, byPlayer: true, cash, 1000, FinanceOperationState.Proposed, PlayerPaidAt)], []);

        claim.CurrentFee.ShouldBeNull();
    }

    [Fact]
    public async Task RestoreFinance_AfterInterruptedRun_FixesFeeAndMarksWithoutHistory()
    {
        // Прошлый вызов вставил операцию и оборвался до операции над заявкой.
        AddFeeSetting(new DateTime(2026, 1, 1), 1000);
        var (claim, restorer, _, cash) = CreateClaimForFinance();
        var operation = Operation(600, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt);
        _ = await WriteRepository.InsertRestoredFinanceOperation(new RestoredFinanceOperationRow(
            claim.GetId(), 600, operation.Author, false, FinanceOperationType.Submit, 1000, operation.PaymentType,
            FinanceOperationState.Approved, MasterAcceptedAt.UtcDateTime, MasterAcceptedAt.UtcDateTime, new DateTime(2026, 7, 2), "Оплата"));
        var before = DateTime.UtcNow;

        var result = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), [operation], []);

        result.Operations.ShouldBe([new RestoredFinanceEntry(RestoredFinanceEntryStatus.AlreadyExisted, 600)]);
        RestoreComments(claim).ShouldBeEmpty();
        claim.CurrentFee.ShouldBe(1000);
        claim.LastMasterCommentAt.ShouldBe(MasterAcceptedAt);
        claim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(before);
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RestoreFinance_ConflictingIds_AreReported_AndTheirModerationsSkipped()
    {
        var (claim, restorer, otherMaster, cash) = CreateClaimForFinance();
        var otherClaim = mock.CreateClaim(mock.CreateCharacter("Петя"), mock.Player);
        mock.CreateComment(otherClaim, "Чужой комментарий").CommentId = 650;
        var notLost = FakeCharacterAggregateWriteRepository.CommentIdentityCurrent + 5;

        var result = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(),
        [
            Operation(650, mock.Player, byPlayer: true, cash, 500, FinanceOperationState.Approved, PlayerPaidAt, ModeratedAt),
            Operation(notLost, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt),
        ],
        [Moderation(null, otherMaster, approved: true, ModeratedAt, parentOperationId: 650)]);

        result.Operations.ShouldBe(
        [
            new RestoredFinanceEntry(RestoredFinanceEntryStatus.TakenElsewhere, 650),
            new RestoredFinanceEntry(RestoredFinanceEntryStatus.NotLostId, notLost),
        ]);
        result.Moderations.ShouldBe([new RestoredFinanceEntry(RestoredFinanceEntryStatus.ParentNotRestored, null)]);
        result.Conflicts.ShouldBe(3);
        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        claim.FinanceOperations.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestoreFinance_InactivePaymentType_IsAllowed()
    {
        var (claim, restorer, _, cash) = CreateClaimForFinance();
        cash.IsActive = false;
        mock.ReInitProjectInfo();

        var result = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(),
            [Operation(600, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt)], []);

        result.Inserted.ShouldBe(1);
    }

    [Fact]
    public async Task RestoreFinance_MasterWithoutClaimsPermission_ButWithMoney_CanRestore()
    {
        var (claim, restorer, _, cash) = CreateClaimForFinance();
        mock.Project.ProjectAcls.Single(a => a.UserId == restorer.UserId).CanManageClaims = false;
        mock.ReInitProjectInfo();

        var result = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(),
            [Operation(600, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt)], []);

        result.Inserted.ShouldBe(1);
    }

    [Fact]
    public async Task RestoreFinance_MasterWithoutMoneyPermission_HasNoAccess()
    {
        var (claim, restorer, _, cash) = CreateClaimForFinance();
        mock.Project.ProjectAcls.Single(a => a.UserId == restorer.UserId).CanManageMoney = false;
        mock.ReInitProjectInfo();

        await ShouldRejectFinance<NoAccessToProjectException>(claim, restorer.UserId,
            [Operation(600, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt)], []);
    }

    [Fact]
    public async Task RestoreFinance_Player_HasNoAccess()
    {
        var (claim, _, _, cash) = CreateClaimForFinance();

        await ShouldRejectFinance<NoAccessToProjectException>(claim, mock.Player.UserId,
            [Operation(600, mock.Player, byPlayer: true, cash, 1000, FinanceOperationState.Proposed, PlayerPaidAt)], []);
    }

    [Fact]
    public async Task RestoreFinance_ActiveProject_Throws_AndNothingIsInserted()
    {
        var cash = mock.CreateCashPaymentType(mock.Master);
        var claim = mock.CreateClaim(mock.CreateCharacter("Вася"), mock.Player);
        mock.ReInitProjectInfo();

        await ShouldRejectFinance<JoinValidationException>(claim, null,
            [Operation(600, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt)], []);
    }

    public static TheoryData<string> InvalidOperations => ["знак взноса", "знак возврата", "нулевая сумма", "неподтверждённый возврат",
        "изменена раньше создания", "из будущего", "перевод", "дубль id", "неизвестный автор"];

    [Theory]
    [MemberData(nameof(InvalidOperations))]
    public async Task RestoreFinance_InvalidOperation_Throws_AndNothingIsInserted(string kind)
    {
        var (claim, restorer, _, cash) = CreateClaimForFinance();
        var valid = Operation(600, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt);
        var (invalid, expected) = kind switch
        {
            "знак взноса" => (valid with { OperationId = 601, Money = -1000 }, typeof(ArgumentException)),
            "знак возврата" => (valid with { OperationId = 601, Type = FinanceOperationType.Refund, Money = 1000 }, typeof(ArgumentException)),
            "нулевая сумма" => (valid with { OperationId = 601, Money = 0 }, typeof(ArgumentException)),
            "неподтверждённый возврат" => (valid with { OperationId = 601, Type = FinanceOperationType.Refund, Money = -100, State = FinanceOperationState.Proposed }, typeof(ArgumentException)),
            "изменена раньше создания" => (valid with { OperationId = 601, ChangedAt = MasterAcceptedAt.AddDays(-1) }, typeof(ArgumentException)),
            "из будущего" => (valid with { OperationId = 601, CreatedAt = DateTimeOffset.UtcNow.AddDays(10), ChangedAt = DateTimeOffset.UtcNow.AddDays(10) }, typeof(CannotPerformOperationInFuture)),
            "перевод" => (valid with { OperationId = 601, Type = FinanceOperationType.TransferFrom }, typeof(ArgumentException)),
            "дубль id" => (valid, typeof(ArgumentException)),
            "неизвестный автор" => (valid with { OperationId = 601, Author = new UserIdentification(100500) }, typeof(JoinRpgEntityNotFoundException)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        var exception = await Should.ThrowAsync<Exception>(
            () => CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), [valid, invalid], []));

        exception.GetType().ShouldBe(expected);

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        claim.FinanceOperations.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestoreFinance_OnlinePaymentType_Throws()
    {
        var (claim, restorer, _, _) = CreateClaimForFinance();
        var online = new PaymentType(PaymentTypeKind.Online, mock.Project.ProjectId, mock.Master.UserId)
        {
            PaymentTypeId = mock.Project.PaymentTypes.Count + 1,
            Project = mock.Project,
            User = mock.Master,
            Operations = [],
        };
        mock.Project.PaymentTypes.Add(online);
        mock.ReInitProjectInfo();

        await ShouldRejectFinance<ArgumentException>(claim, restorer.UserId,
            [Operation(600, mock.Player, byPlayer: true, online, 1000, FinanceOperationState.Approved, PlayerPaidAt)], []);
    }

    [Fact]
    public async Task RestoreFinance_UnknownPaymentType_Throws()
    {
        var (claim, restorer, _, cash) = CreateClaimForFinance();
        var operation = Operation(600, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt);

        await ShouldRejectFinance<JoinValidationException>(claim, restorer.UserId,
            [operation with { PaymentType = new PaymentTypeIdentification(ProjectId, 100500) }], []);
    }

    public static TheoryData<string> InvalidModerations => ["без родителя из будущего", "оба родителя", "индекс вне списка", "индекс операции с id",
        "чужая операция", "раньше операции", "дубль id"];

    [Theory]
    [MemberData(nameof(InvalidModerations))]
    public async Task RestoreFinance_InvalidModeration_Throws_AndNothingIsInserted(string kind)
    {
        var (claim, restorer, otherMaster, cash) = CreateClaimForFinance();
        var otherClaim = mock.CreateClaim(mock.CreateCharacter("Петя"), mock.Player);
        _ = AddExistingOperation(otherClaim, 650, FinanceOperationType.Submit, 100, FinanceOperationState.Proposed, cash);
        RestoredFinanceOperation[] operations =
        [
            Operation(600, mock.Player, byPlayer: true, cash, 500, FinanceOperationState.Approved, PlayerPaidAt, ModeratedAt),
            Operation(null, mock.Player, byPlayer: true, cash, 700, FinanceOperationState.Approved, PlayerPaidAt, ModeratedAt),
        ];
        var (moderation, expected) = kind switch
        {
            "без родителя из будущего" => (Moderation(601, otherMaster, true, DateTimeOffset.UtcNow.AddDays(10)), typeof(CannotPerformOperationInFuture)),
            "оба родителя" => (Moderation(601, otherMaster, true, ModeratedAt, parentOperationId: 600, parentRestoredIndex: 1), typeof(ArgumentException)),
            "индекс вне списка" => (Moderation(601, otherMaster, true, ModeratedAt, parentRestoredIndex: 2), typeof(ArgumentException)),
            "индекс операции с id" => (Moderation(601, otherMaster, true, ModeratedAt, parentRestoredIndex: 0), typeof(ArgumentException)),
            "чужая операция" => (Moderation(601, otherMaster, true, ModeratedAt, parentOperationId: 650), typeof(JoinValidationException)),
            "раньше операции" => (Moderation(601, otherMaster, true, PlayerPaidAt.AddDays(-1), parentOperationId: 600), typeof(ArgumentException)),
            "дубль id" => (Moderation(600, otherMaster, true, ModeratedAt, parentRestoredIndex: 1), typeof(ArgumentException)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        var exception = await Should.ThrowAsync<Exception>(
            () => CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), operations, [moderation]));

        exception.GetType().ShouldBe(expected);

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        claim.FinanceOperations.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RestoreFinance_ClaimMissing_Throws()
    {
        var (_, restorer, _, cash) = CreateClaimForFinance();

        _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => CreateService(restorer.UserId).RestoreFinanceOperations(new ClaimIdentification(ProjectId, 100500),
                [Operation(600, mock.Master, byPlayer: false, cash, 1000, FinanceOperationState.Approved, MasterAcceptedAt)], []));

        mock.Project.Claims.ShouldAllBe(c => c.FinanceOperations.Count == 0);
    }

    [Fact]
    public async Task RestoreFinance_EmptyInput_DoesNothing()
    {
        var (claim, restorer, _, _) = CreateClaimForFinance();

        var result = await CreateService(restorer.UserId).RestoreFinanceOperations(claim.GetId(), [], []);

        result.Operations.ShouldBeEmpty();
        result.Moderations.ShouldBeEmpty();
        claim.LastUpdateDateTime.ShouldBe(BackupAt.UtcDateTime);
        SaveChangesCallCount.ShouldBe(0);
    }

    private async Task ShouldRejectFinance<TException>(
        Claim claim, int? currentUserId, IReadOnlyList<RestoredFinanceOperation> operations, IReadOnlyList<RestoredFinanceModeration> moderations)
        where TException : Exception
    {
        _ = await Should.ThrowAsync<TException>(
            () => CreateService(currentUserId).RestoreFinanceOperations(claim.GetId(), operations, moderations));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        claim.FinanceOperations.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
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
