using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Юнит-тесты методов <see cref="ClaimServiceImpl"/>, переведённых на
/// <c>ICharacterPropsService</c> (ADR014). До миграции тестов у сервиса не было вовсе.
/// </summary>
public class ClaimServiceImplTest : ClaimServiceTestBase
{
    private ClaimServiceImpl CreateService(int? currentUserId = null)
    {
        var currentUser = CreateCurrentUser(currentUserId);
        return new ClaimServiceImpl(
            unitOfWork,
            currentUser,
            metadataRepository,
            new FakeProblemValidator<Claim>(),
            NullLogger<CharacterServiceImpl>.Instance,
            CreatePropsService(currentUserId),
            // Собственный экземпляр props-сервиса — как и в бою, где оба транзиентны.
            new ClaimApprovalService(CreatePropsService(currentUserId)),
            CreateAutoApproveService());
    }

    private Claim CreateClaim(ClaimStatus status, string characterName = "Вася")
    {
        var character = mock.CreateCharacter(characterName);
        var claim = mock.CreateClaim(character, mock.Player);
        claim.ClaimStatus = status;
        mock.ReInitProjectInfo();
        return claim;
    }

    #region OnHoldByMaster

    [Fact]
    public async Task OnHoldByMaster_ChangesStatus_SavesOnce_AndNotifiesOnce()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await CreateService().OnHoldByMaster(claim.GetId(), "подождём");

        claim.ClaimStatus.ShouldBe(ClaimStatus.OnHold);
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task OnHoldByMaster_ByPlayer_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).OnHoldByMaster(claim.GetId(), "подождём"));

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    #endregion

    #region DeclineByPlayer

    [Fact]
    public async Task DeclineByPlayer_ChangesStatus_SavesOnce_AndNotifiesOnce()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await CreateService(mock.Player.UserId).DeclineByPlayer(claim.GetId(), "передумал");

        claim.ClaimStatus.ShouldBe(ClaimStatus.DeclinedByUser);
        claim.PlayerDeclinedDate.ShouldNotBeNull();
        claim.PlayerAllowedSenstiveData.ShouldBeFalse();
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DeclineByPlayer_ByMaster_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        _ = await Should.ThrowAsync<PlayerOnlyException>(
            () => CreateService().DeclineByPlayer(claim.GetId(), "передумал"));

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    #endregion

    #region DeclineByMaster

    /// <summary>
    /// Заводит заявке поселение в комнате — тогда отказ порождает письмо легаси-канала.
    /// </summary>
    private AccommodationRequest CreateAccommodation(Claim claim)
    {
        var request = mock.CreateAccommodationRequest(mock.CreateAccommodationType(), claim);
        _ = mock.CreateRoom(request);
        return request;
    }

    [Fact]
    public async Task DeclineByMaster_ChangesStatus_SavesOnce_AndNotifiesOnce()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        claim.PlayerAllowedSenstiveData = true;

        await CreateService().DeclineByMaster(
            claim.GetId(), ClaimDenialReason.NotSuitable, "не подходит", deleteCharacter: false);

        claim.ClaimStatus.ShouldBe(ClaimStatus.DeclinedByMaster);
        claim.MasterDeclinedDate.ShouldNotBeNull();
        claim.ClaimDenialStatus.ShouldBe(ClaimDenialReason.NotSuitable);
        claim.PlayerAllowedSenstiveData.ShouldBeFalse();
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DeclineByMaster_OfApprovedClaim_ClearsApprovedClaim()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        mock.ReInitProjectInfo();

        await CreateService().DeclineByMaster(
            claim.GetId(), ClaimDenialReason.Refused, "отказ", deleteCharacter: false);

        claim.ClaimStatus.ShouldBe(ClaimStatus.DeclinedByMaster);
        character.ApprovedClaimId.ShouldBeNull();
        character.IsActive.ShouldBeTrue();
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task DeclineByMaster_ByPlayer_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).DeclineByMaster(
                claim.GetId(), ClaimDenialReason.Refused, "отказ", deleteCharacter: false));

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    /// <summary>
    /// Удалять персонажа можно только вместе с отказом по утверждённой заявке — иначе персонажа
    /// «под заявкой» просто нет.
    /// </summary>
    [Fact]
    public async Task DeclineByMaster_DeletingCharacterOfNotApprovedClaim_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        claim.Character.DirectlyRelatedPlotElements = [];

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateService().DeclineByMaster(
                claim.GetId(), ClaimDenialReason.Removed, "удаляем", deleteCharacter: true));

        claim.Character.IsActive.ShouldBeTrue();
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeclineByMaster_DeletingCharacterOfApprovedClaim_DeactivatesCharacter()
    {
        var character = mock.CreateCharacter("Вася");
        character.DirectlyRelatedPlotElements = [];
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        mock.ReInitProjectInfo();

        await CreateService().DeclineByMaster(
            claim.GetId(), ClaimDenialReason.Removed, "удаляем", deleteCharacter: true);

        character.IsActive.ShouldBeFalse();
        character.ApprovedClaimId.ShouldBeNull();
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    /// <summary>
    /// Письмо о выезде из комнаты уходит вторым каналом и строго ПОСЛЕ уведомления — так было до
    /// миграции, и порядок остаётся частью контракта (ADR014, §3).
    /// </summary>
    [Fact]
    public async Task DeclineByMaster_WithAccommodation_SendsLeaveRoomEmailAfterNotification()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var accommodationRequest = CreateAccommodation(claim);

        await CreateService().DeclineByMaster(
            claim.GetId(), ClaimDenialReason.Refused, "отказ", deleteCharacter: false);

        accommodationRequest.Subjects.ShouldBeEmpty();
        SentEmails.ShouldHaveSingleItem().ShouldBeOfType<LeaveRoomEmail>()
            .Initiator.ShouldBe(mock.Master);

        SentInOrder.Count.ShouldBe(2);
        _ = SentInOrder[0].ShouldBeOfType<ClaimSimpleChangedNotification>();
        _ = SentInOrder[1].ShouldBeOfType<LeaveRoomEmail>();
    }

    #endregion

    #region RestoreByMaster

    [Fact]
    public async Task RestoreByMaster_MovesClaimToAnotherCharacter_SavesOnce_AndNotifiesOnce()
    {
        var claim = CreateClaim(ClaimStatus.DeclinedByMaster);
        claim.ClaimDenialStatus = ClaimDenialReason.Removed;
        var target = mock.CreateCharacter("Петя");
        target.IsActive = false;
        mock.ReInitProjectInfo();

        await CreateService().RestoreByMaster(claim.GetId(), "вернём", target.GetId());

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByMaster);
        claim.ClaimDenialStatus.ShouldBeNull();
        claim.Character.ShouldBe(target);
        target.IsActive.ShouldBeTrue();
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    /// <summary>
    /// Персонаж, куда восстанавливают заявку, уже занят другой утверждённой заявкой.
    /// </summary>
    [Fact]
    public async Task RestoreByMaster_ToOccupiedCharacter_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.DeclinedByMaster);
        var target = mock.CreateCharacter("Петя");
        _ = mock.CreateApprovedClaim(target, mock.Master);
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<ClaimTargetIsNotAcceptingClaims>(
            () => CreateService().RestoreByMaster(claim.GetId(), "вернём", target.GetId()));

        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task RestoreByMaster_ByPlayer_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.DeclinedByMaster);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId)
                .RestoreByMaster(claim.GetId(), "вернём", claim.Character.GetId()));

        claim.ClaimStatus.ShouldBe(ClaimStatus.DeclinedByMaster);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    #endregion

    #region ApproveByMaster

    [Fact]
    public async Task ApproveByMaster_ApprovesClaim_SavesOnce_AndNotifiesOnce()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        claim.Character.IsHot = true;

        await CreateService().ApproveByMaster(claim.GetId(), "принято");

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.MasterAcceptedDate.ShouldNotBeNull();
        claim.Character.ApprovedClaimId.ShouldBe(claim.ClaimId);
        claim.Character.ApprovedClaim.ShouldBe(claim);
        claim.Character.IsHot.ShouldBeFalse();
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ApproveByMaster_OfCheckedInClaim_Throws_AndDoesNotSave()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateCheckedInClaim(character, mock.Player);
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<ClaimWrongStatusException>(
            () => CreateService().ApproveByMaster(claim.GetId(), "принято"));

        claim.ClaimStatus.ShouldBe(ClaimStatus.CheckedIn);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApproveByMaster_ByPlayer_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).ApproveByMaster(claim.GetId(), "принято"));

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    /// <summary>
    /// Автоотклонение прочих заявок того же игрока читает навигацию <c>claim.Player.Claims</c>.
    /// Если её не загрузить (в бою — <c>Include(c =&gt; c.Player.Claims)</c>), список окажется пуст
    /// и автоотклонение тихо исчезнет — без единой ошибки. Поэтому тест обязателен.
    /// </summary>
    [Fact]
    public async Task ApproveByMaster_WithStrictlyOneCharacter_DeclinesOtherPendingClaimsOfSamePlayer()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var otherClaim = CreateClaim(ClaimStatus.AddedByUser, "Петя");

        // По умолчанию у мока EnableManyCharacters == false, то есть StrictlyOneCharacter == true.
        mock.ProjectInfo.ClaimSettings.StrictlyOneCharacter.ShouldBeTrue();

        await CreateService().ApproveByMaster(claim.GetId(), "принято");

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        otherClaim.ClaimStatus.ShouldBe(ClaimStatus.DeclinedByMaster);
        otherClaim.MasterDeclinedDate.ShouldNotBeNull();

        SaveChangesCallCount.ShouldBe(1);

        // Порядок значим: сначала уведомление по утверждённой заявке, потом по автоотклонённой.
        SentNotifications.Count.ShouldBe(2);
        SentNotifications[0].ShouldBeOfType<ClaimSimpleChangedNotification>().ClaimId.ShouldBe(claim.GetId());
        SentNotifications[1].ShouldBeOfType<ClaimSimpleChangedNotification>().ClaimId.ShouldBe(otherClaim.GetId());
    }

    [Fact]
    public async Task ApproveByMaster_WithManyCharactersAllowed_KeepsOtherPendingClaims()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var otherClaim = CreateClaim(ClaimStatus.AddedByUser, "Петя");
        mock.Project.Details.EnableManyCharacters = true;
        mock.ReInitProjectInfo();

        await CreateService().ApproveByMaster(claim.GetId(), "принято");

        otherClaim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        SentNotifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ApproveByMaster_OfClaimToSlot_CreatesCharacterFromSlot()
    {
        var slot = mock.CreateSlot("Слот", slotLimit: 3);
        var claim = mock.CreateClaim(slot, mock.Player);
        claim.ClaimStatus = ClaimStatus.AddedByUser;
        var plot = mock.CreatePlotElement(slot);
        mock.ReInitProjectInfo();

        await CreateService().ApproveByMaster(claim.GetId(), "принято");

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);

        var created = claim.Character;
        created.ShouldNotBe(slot);
        created.AutoCreated.ShouldBeTrue();
        created.OriginalCharacterSlot.ShouldBe(slot);
        created.CharacterType.ShouldBe(CharacterType.Player);
        created.CharacterSlotLimit.ShouldBeNull();
        created.ApprovedClaim.ShouldBe(claim);
        created.IsHot.ShouldBeFalse();

        slot.CharacterSlotLimit.ShouldBe(2);
        mock.Project.Characters.ShouldContain(created);

        // Новый персонаж наследует прямые привязки слота к сюжетам.
        plot.TargetCharacters.ShouldContain(created);

        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ApproveByMaster_OfClaimToExhaustedSlot_Throws_AndDoesNotSave()
    {
        var slot = mock.CreateSlot("Слот", slotLimit: 0);
        var claim = mock.CreateClaim(slot, mock.Player);
        claim.ClaimStatus = ClaimStatus.AddedByUser;
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<JoinRpgSlotLimitedException>(
            () => CreateService().ApproveByMaster(claim.GetId(), "принято"));

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        claim.Character.ShouldBe(slot);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    #endregion

    #region Создание заявки

    /// <summary>
    /// Персонаж, на которого можно подать заявку, вместе с пустым слоем полей, построенным по
    /// актуальному снимку метаданных.
    /// </summary>
    private (Character Character, FieldLayerContainer Fields) CreateTarget(string name = "Вася")
    {
        var character = mock.CreateCharacter(name);
        mock.ReInitProjectInfo();
        return (character, FieldLayerContainer.Empty(mock.ProjectInfo));
    }

    [Fact]
    public async Task AddClaimFromUser_CreatesClaim_SavesTwice_AndNotifiesOnce()
    {
        var (character, fields) = CreateTarget();

        var claimId = await CreateService(mock.Player.UserId)
            .AddClaimFromUser(character.GetId(), "хочу играть", fields, sensitiveDataAllowed: false);

        var claim = mock.Project.Claims.ShouldHaveSingleItem();
        claim.GetId().ShouldBe(claimId);
        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        claim.PlayerUserId.ShouldBe(mock.Player.UserId);
        claim.Character.ShouldBe(character);
        claim.ResponsibleMasterUserId.ShouldBe(mock.Master.UserId);

        // Связка нужна FieldSaveHelper.MarkUsed: он читает project.ProjectFields и без неё падает.
        claim.Project.ShouldBe(mock.Project);

        var comment = claim.CommentDiscussion.Comments.ShouldHaveSingleItem();
        comment.ExtraAction.ShouldBe(CommentExtraAction.NewClaim);
        comment.IsCommentByPlayer.ShouldBeTrue();

        SaveChangesCallCount.ShouldBe(2);
        SentNotifications.Count.ShouldBe(1);
    }

    /// <summary>
    /// Сохранений ровно два, и это не формальность: комментарий обязан создаваться <b>между</b>
    /// ними. До первого сохранения у дискуссии <c>CommentDiscussionId == -1</c>, и комментарий,
    /// созданный раньше, уехал бы в несуществующую дискуссию. Тест ловит попытку «схлопнуть» два
    /// сохранения в одно.
    /// </summary>
    [Fact]
    public async Task AddClaimFromUser_CreatesCommentBetweenTwoSaves()
    {
        var (character, fields) = CreateTarget();

        var commentsAtSave = new List<int>();
        OnSaveChanges = _ => commentsAtSave.Add(
            mock.Project.Claims.Sum(claim => claim.CommentDiscussion.Comments.Count));

        _ = await CreateService(mock.Player.UserId)
            .AddClaimFromUser(character.GetId(), "хочу играть", fields, sensitiveDataAllowed: false);

        commentsAtSave.ShouldBe([0, 1]);
    }

    /// <summary>
    /// Разрешение на чувствительные данные запоминается, только если проект его вообще спрашивает.
    /// </summary>
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public async Task AddClaimFromUser_StoresSensitiveDataPermission(
        bool sensitiveDataAllowed, bool projectRequiresSensitiveData, bool expected)
    {
        mock.Project.Details.RequirePassport = projectRequiresSensitiveData
            ? MandatoryStatus.Required
            : MandatoryStatus.Optional;
        var (character, fields) = CreateTarget();

        _ = await CreateService(mock.Player.UserId)
            .AddClaimFromUser(character.GetId(), "хочу играть", fields, sensitiveDataAllowed);

        mock.Project.Claims.ShouldHaveSingleItem().PlayerAllowedSenstiveData.ShouldBe(expected);
    }

    [Fact]
    public async Task AddClaimFromMaster_CreatesClaimForGivenPlayer_SavesTwice_AndNotifiesOnce()
    {
        var (character, fields) = CreateTarget();

        var claimId = await CreateService()
            .AddClaimFromMaster(character.GetId(), mock.Player.GetId(), "приглашаю", fields);

        var claim = mock.Project.Claims.ShouldHaveSingleItem();
        claim.GetId().ShouldBe(claimId);
        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByMaster);

        // Заявка оформляется на игрока, а не на мастера, который её создал.
        claim.PlayerUserId.ShouldBe(mock.Player.UserId);
        claim.PlayerUserId.ShouldNotBe(mock.Master.UserId);

        // Мастер не может дать разрешение на чувствительные данные от имени игрока.
        claim.PlayerAllowedSenstiveData.ShouldBeFalse();

        var comment = claim.CommentDiscussion.Comments.ShouldHaveSingleItem();
        comment.ExtraAction.ShouldBe(CommentExtraAction.NewClaim);
        comment.IsCommentByPlayer.ShouldBeFalse();

        SaveChangesCallCount.ShouldBe(2);
        SentNotifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AddClaimFromMaster_WithoutManageClaims_Throws_AndDoesNotSave()
    {
        var (character, fields) = CreateTarget();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId)
                .AddClaimFromMaster(character.GetId(), mock.Player.GetId(), "приглашаю", fields));

        mock.Project.Claims.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    /// <summary>
    /// Закрытый приём заявок — причина с <c>MasterCanOverride</c>: игрока она останавливает,
    /// мастера нет.
    /// </summary>
    [Fact]
    public async Task AddClaimFromUser_WhenProjectClaimsClosed_Throws_AndDoesNotSave()
    {
        mock.Project.IsAcceptingClaims = false;
        var (character, fields) = CreateTarget();

        _ = await Should.ThrowAsync<ClaimTargetIsNotAcceptingClaims>(
            () => CreateService(mock.Player.UserId)
                .AddClaimFromUser(character.GetId(), "хочу играть", fields, sensitiveDataAllowed: false));

        mock.Project.Claims.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddClaimFromMaster_WhenProjectClaimsClosed_StillCreatesClaim()
    {
        mock.Project.IsAcceptingClaims = false;
        var (character, fields) = CreateTarget();

        _ = await CreateService()
            .AddClaimFromMaster(character.GetId(), mock.Player.GetId(), "приглашаю", fields);

        mock.Project.Claims.ShouldHaveSingleItem().ClaimStatus.ShouldBe(ClaimStatus.AddedByMaster);
        SaveChangesCallCount.ShouldBe(2);
    }

    /// <summary>
    /// Занятость роли мастер обойти не может — у этой причины <c>MasterCanOverride == false</c>.
    /// </summary>
    [Fact]
    public async Task AddClaimFromMaster_ToBusyCharacter_Throws_AndDoesNotSave()
    {
        var character = mock.CreateCharacter("Вася");
        _ = mock.CreateApprovedClaim(character, mock.Master);
        mock.ReInitProjectInfo();
        var fields = FieldLayerContainer.Empty(mock.ProjectInfo);

        _ = await Should.ThrowAsync<ClaimTargetIsNotAcceptingClaims>(
            () => CreateService()
                .AddClaimFromMaster(character.GetId(), mock.Player.GetId(), "приглашаю", fields));

        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    /// <summary>
    /// Вторая заявка того же игрока на того же персонажа не создаётся.
    /// </summary>
    [Fact]
    public async Task AddClaimFromUser_WhenAlreadySent_Throws_AndDoesNotSave()
    {
        var character = mock.CreateCharacter("Вася");
        var existing = mock.CreateClaim(character, mock.Player);
        existing.ClaimStatus = ClaimStatus.AddedByUser;
        mock.ReInitProjectInfo();
        var fields = FieldLayerContainer.Empty(mock.ProjectInfo);

        _ = await Should.ThrowAsync<ClaimAlreadyPresentException>(
            () => CreateService(mock.Player.UserId)
                .AddClaimFromUser(character.GetId(), "ещё раз", fields, sensitiveDataAllowed: false));

        mock.Project.Claims.ShouldHaveSingleItem().ShouldBe(existing);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    #endregion

    #region Автоприём

    /// <summary>
    /// Автоприём — отдельная операция ПОСЛЕ создания заявки, а не вложенная в него (ADR014, §7).
    /// Проверяем именно момент вызова: к этому времени оба сохранения создания уже прошли и
    /// уведомление о новой заявке уже ушло.
    /// </summary>
    [Fact]
    public async Task AddClaimFromUser_WithAutoAccept_ApprovesAfterCreationCompleted()
    {
        mock.Project.Details.AutoAcceptClaims = true;
        var (character, fields) = CreateTarget();

        var savesAtApprove = -1;
        var notificationsAtApprove = -1;
        autoApprovals.OnApprove = () =>
        {
            savesAtApprove = SaveChangesCallCount;
            notificationsAtApprove = SentNotifications.Count;
        };

        var claimId = await CreateService(mock.Player.UserId)
            .AddClaimFromUser(character.GetId(), "хочу играть", fields, sensitiveDataAllowed: false);

        autoApprovals.Calls.ShouldHaveSingleItem().ClaimId.ShouldBe(claimId);

        // Создание заявки — два сохранения (ADR014); автоприём начинается строго после них.
        savesAtApprove.ShouldBe(2);
        notificationsAtApprove.ShouldBe(1);

        // Утверждает ответственный мастер, а не подавший заявку игрок.
        impersonateAccessor.Impersonated.ShouldBe([mock.Master.GetId()]);
        impersonateAccessor.StopCount.ShouldBe(1);
    }

    /// <summary>
    /// Если проект требует доступ к чувствительным данным, а игрок его не дал, автоприём молчит.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task AddClaimFromUser_WhenSensitiveDataRequired_AutoApprovesOnlyIfAllowed(
        bool sensitiveDataAllowed, bool expectedAutoApprove)
    {
        mock.Project.Details.AutoAcceptClaims = true;
        mock.Project.Details.RequirePassport = MandatoryStatus.Required;
        var (character, fields) = CreateTarget();

        _ = await CreateService(mock.Player.UserId)
            .AddClaimFromUser(character.GetId(), "хочу играть", fields, sensitiveDataAllowed);

        autoApprovals.Calls.Count.ShouldBe(expectedAutoApprove ? 1 : 0);
        impersonateAccessor.Impersonated.Count.ShouldBe(expectedAutoApprove ? 1 : 0);
    }

    [Fact]
    public async Task AddClaimFromUser_WithoutAutoAccept_DoesNotApprove()
    {
        var (character, fields) = CreateTarget();

        _ = await CreateService(mock.Player.UserId)
            .AddClaimFromUser(character.GetId(), "хочу играть", fields, sensitiveDataAllowed: false);

        autoApprovals.Calls.ShouldBeEmpty();
    }

    #endregion

    #region AcceptInvitation

    [Fact]
    public async Task AcceptInvitation_MovesToDiscussed_SavesOnce_AndNotifiesOnce()
    {
        var claim = CreateClaim(ClaimStatus.AddedByMaster);

        await CreateService(mock.Player.UserId)
            .AcceptInvitation(claim.GetId(), "согласен", sensitiveDataAllowed: false);

        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
        claim.CommentDiscussion.Comments.ShouldHaveSingleItem()
            .ExtraAction.ShouldBe(CommentExtraAction.InvitationAcceptedByPlayer);
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    /// <summary>
    /// Принять можно только приглашение мастера. Проверка именно такая — своя, а не через таблицу
    /// переходов статусов.
    /// </summary>
    [Fact]
    public async Task AcceptInvitation_OfClaimAddedByPlayer_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        _ = await Should.ThrowAsync<ClaimWrongStatusException>(
            () => CreateService(mock.Player.UserId)
                .AcceptInvitation(claim.GetId(), "согласен", sensitiveDataAllowed: false));

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task AcceptInvitation_ByMaster_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByMaster);

        _ = await Should.ThrowAsync<PlayerOnlyException>(
            () => CreateService().AcceptInvitation(claim.GetId(), "согласен", sensitiveDataAllowed: false));

        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    /// <summary>Разрешение запоминается, только если проект его вообще спрашивает.</summary>
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public async Task AcceptInvitation_StoresSensitiveDataPermission(
        bool sensitiveDataAllowed, bool projectRequiresSensitiveData, bool expected)
    {
        mock.Project.Details.RequirePassport = projectRequiresSensitiveData
            ? MandatoryStatus.Required
            : MandatoryStatus.Optional;
        var claim = CreateClaim(ClaimStatus.AddedByMaster);

        await CreateService(mock.Player.UserId)
            .AcceptInvitation(claim.GetId(), "согласен", sensitiveDataAllowed);

        claim.PlayerAllowedSenstiveData.ShouldBe(expected);
    }

    [Fact]
    public async Task AcceptInvitation_WithAutoAccept_ApprovesAfterNotification()
    {
        mock.Project.Details.AutoAcceptClaims = true;
        var claim = CreateClaim(ClaimStatus.AddedByMaster);

        var savesAtApprove = -1;
        var notificationsAtApprove = -1;
        autoApprovals.OnApprove = () =>
        {
            savesAtApprove = SaveChangesCallCount;
            notificationsAtApprove = SentNotifications.Count;
        };

        await CreateService(mock.Player.UserId)
            .AcceptInvitation(claim.GetId(), "согласен", sensitiveDataAllowed: false);

        autoApprovals.Calls.ShouldHaveSingleItem().ClaimId.ShouldBe(claim.GetId());
        savesAtApprove.ShouldBe(1);
        notificationsAtApprove.ShouldBe(1);
    }

    #endregion

    #region AllowSensitiveData

    [Fact]
    public async Task AllowSensitiveData_SetsFlag_SavesOnce_AndSendsNothing()
    {
        var claim = CreateClaim(ClaimStatus.AddedByMaster);

        await CreateService(mock.Player.UserId).AllowSensitiveData(claim.GetId());

        claim.PlayerAllowedSenstiveData.ShouldBeTrue();
        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task AllowSensitiveData_ByMaster_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByMaster);

        _ = await Should.ThrowAsync<PlayerOnlyException>(
            () => CreateService().AllowSensitiveData(claim.GetId()));

        claim.PlayerAllowedSenstiveData.ShouldBeFalse();
        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion

    #region SetResponsible

    [Fact]
    public async Task SetResponsible_ToAnotherMaster_ChangesAndNotifies()
    {
        var newMaster = mock.CreateMaster();
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await CreateService().SetResponsible(claim.GetId(), newMaster.GetId());

        claim.ResponsibleMasterUserId.ShouldBe(newMaster.UserId);
        claim.CommentDiscussion.Comments.ShouldHaveSingleItem()
            .ExtraAction.ShouldBe(CommentExtraAction.ChangeResponsible);

        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.ShouldHaveSingleItem()
            .ShouldBeOfType<ClaimSimpleChangedNotification>()
            .OldResponsibleMaster.ShouldBe(mock.Master.GetId());
    }

    /// <summary>
    /// Назначение ответственным того же мастера — не операция: ни сохранения, ни уведомления.
    /// До миграции это был ранний <c>return</c> до мутации, и поведение сохранено.
    /// </summary>
    [Fact]
    public async Task SetResponsible_ToSameMaster_DoesNothing()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await CreateService().SetResponsible(claim.GetId(), mock.Master.GetId());

        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
        claim.CommentDiscussion.Comments.ShouldBeEmpty();
    }

    /// <summary>Ответственным можно назначить только мастера проекта.</summary>
    [Fact]
    public async Task SetResponsible_ToNonMaster_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService().SetResponsible(claim.GetId(), mock.Player.GetId()));

        claim.ResponsibleMasterUserId.ShouldBe(mock.Master.UserId);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetResponsible_ByPlayer_Throws_AndDoesNotSave()
    {
        var newMaster = mock.CreateMaster();
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).SetResponsible(claim.GetId(), newMaster.GetId()));

        claim.ResponsibleMasterUserId.ShouldBe(mock.Master.UserId);
        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion

    #region AddComment

    /// <summary>
    /// Заявка чужого игрока: <c>mock.Player</c> для неё — посторонний, но моку известен, поэтому
    /// годится в качестве «пользователя без прав».
    /// </summary>
    private Claim CreateClaimOfAnotherPlayer()
    {
        var otherPlayer = new User
        {
            UserId = 777,
            PrefferedName = "Чужой игрок",
            Email = "stranger@example.com",
            Claims = [],
        };
        var character = mock.CreateCharacter("Чужой");
        var claim = mock.CreateClaim(character, otherPlayer);
        claim.ClaimStatus = ClaimStatus.AddedByUser;
        mock.ReInitProjectInfo();
        return claim;
    }

    [Fact]
    public async Task AddComment_ByMaster_AddsComment_SavesOnce_AndNotifiesOnce()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await CreateService().AddComment(
            claim.GetId(), parentCommentId: null, isVisibleToPlayer: true, "как дела?", FinanceOperationAction.None);

        var comment = claim.CommentDiscussion.Comments.ShouldHaveSingleItem();
        comment.IsVisibleToPlayer.ShouldBeTrue();
        comment.Parent.ShouldBeNull();

        // Мастер ответил на поданную заявку видимым комментарием — заявка уходит в обсуждение.
        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);

        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AddComment_ByPlayer_IsPlayerChange()
    {
        var claim = CreateClaim(ClaimStatus.AddedByMaster);

        await CreateService(mock.Player.UserId).AddComment(
            claim.GetId(), parentCommentId: null, isVisibleToPlayer: true, "согласен", FinanceOperationAction.None);

        claim.CommentDiscussion.Comments.ShouldHaveSingleItem().IsCommentByPlayer.ShouldBeTrue();
        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
        SentNotifications.ShouldHaveSingleItem()
            .ShouldBeOfType<ClaimSimpleChangedNotification>()
            .ClaimOperationType.ShouldBe(ClaimOperationType.PlayerChange);
    }

    /// <summary>
    /// Единственная claim-операция, разрешённая в архивном проекте (ADR014): обсуждение игры
    /// продолжается после её конца, и форма комментария в UI намеренно не спрятана. Тест держит
    /// это решение — иначе <c>MustBeActive</c> уедет сюда вместе со следующей правкой.
    /// </summary>
    [Fact]
    public async Task AddComment_InArchivedProject_StillWorks()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();

        await CreateService().AddComment(
            claim.GetId(), parentCommentId: null, isVisibleToPlayer: true, "игра кончилась, обсудим", FinanceOperationAction.None);

        claim.CommentDiscussion.Comments.ShouldHaveSingleItem();
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AddComment_ByStranger_Throws_AndDoesNotSave()
    {
        var claim = CreateClaimOfAnotherPlayer();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).AddComment(
                claim.GetId(), parentCommentId: null, isVisibleToPlayer: true, "а вот и я", FinanceOperationAction.None));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddComment_WithFinanceActionWithoutParentComment_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateService().AddComment(
                claim.GetId(), parentCommentId: null, isVisibleToPlayer: true, "принято", FinanceOperationAction.Approve));

        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    /// <summary>
    /// Ответить на скрытый от игрока комментарий так, чтобы игрок ответ увидел, нельзя.
    /// </summary>
    [Fact]
    public async Task AddComment_VisibleReplyToHiddenComment_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var hidden = mock.CreateComment(claim, "только для мастеров", isVisibleToPlayer: false);

        _ = await Should.ThrowAsync<EntityWrongStatusException>(
            () => CreateService().AddComment(
                claim.GetId(), hidden.CommentId, isVisibleToPlayer: true, "отвечаю", FinanceOperationAction.None));

        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddComment_SecretReplyToHiddenComment_Works()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var hidden = mock.CreateComment(claim, "только для мастеров", isVisibleToPlayer: false);

        await CreateService().AddComment(
            claim.GetId(), hidden.CommentId, isVisibleToPlayer: false, "отвечаю", FinanceOperationAction.None);

        claim.CommentDiscussion.Comments.Last().Parent.ShouldBe(hidden);
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    /// <summary>
    /// Комментарий с неодобренной финансовой операцией — то, что модерирует мастер.
    /// </summary>
    private Comment CreateCommentWithProposedPayment(Claim claim, int money = 100)
    {
        var comment = mock.CreateComment(claim, "внёс взнос");
        var finance = new FinanceOperation
        {
            Claim = claim,
            Comment = comment,
            ProjectId = mock.Project.ProjectId,
            MoneyAmount = money,
            State = FinanceOperationState.Proposed,
            OperationType = FinanceOperationType.Submit,
            OperationDate = DateTime.UtcNow,
        };
        comment.Finance = finance;
        claim.FinanceOperations.Add(finance);
        return comment;
    }

    [Fact]
    public async Task AddComment_ApprovingFinance_ApprovesOperation_AndMarksComment()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var parent = CreateCommentWithProposedPayment(claim);

        await CreateService().AddComment(
            claim.GetId(), parent.CommentId, isVisibleToPlayer: true, "ок", FinanceOperationAction.Approve);

        parent.Finance.State.ShouldBe(FinanceOperationState.Approved);
        claim.CommentDiscussion.Comments.Last().ExtraAction.ShouldBe(CommentExtraAction.ApproveFinance);
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AddComment_DecliningFinance_DeclinesOperation()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var parent = CreateCommentWithProposedPayment(claim);

        await CreateService().AddComment(
            claim.GetId(), parent.CommentId, isVisibleToPlayer: true, "нет", FinanceOperationAction.Decline);

        parent.Finance.State.ShouldBe(FinanceOperationState.Declined);
        claim.CommentDiscussion.Comments.Last().ExtraAction.ShouldBe(CommentExtraAction.RejectFinance);
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task AddComment_ModeratingAlreadyModeratedFinance_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var parent = CreateCommentWithProposedPayment(claim);
        parent.Finance.State = FinanceOperationState.Approved;

        _ = await Should.ThrowAsync<ValueAlreadySetException>(
            () => CreateService().AddComment(
                claim.GetId(), parent.CommentId, isVisibleToPlayer: true, "ещё раз", FinanceOperationAction.Approve));

        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    #endregion

    #region MoveByMaster

    [Fact]
    public async Task MoveByMaster_MovesApprovedClaim_AndMovesApprovedClaimMark()
    {
        var oldCharacter = mock.CreateCharacter("Вася");
        var claim = mock.CreateApprovedClaim(oldCharacter, mock.Player);
        var target = mock.CreateCharacter("Петя");
        mock.ReInitProjectInfo();

        await CreateService().MoveByMaster(claim.GetId(), "переносим", target.GetId());

        claim.Character.ShouldBe(target);
        oldCharacter.ApprovedClaim.ShouldBeNull();
        target.ApprovedClaim.ShouldBe(claim);

        SaveChangesCallCount.ShouldBe(1);
        var notification = SentNotifications.ShouldHaveSingleItem().ShouldBeOfType<ClaimSimpleChangedNotification>();
        notification.CommentExtraAction.ShouldBe(CommentExtraAction.MoveByMaster);
        notification.AnotherCharacterId.ShouldBe(oldCharacter.GetId());
    }

    [Fact]
    public async Task MoveByMaster_MovesPendingClaim_WithoutTouchingApprovedClaim()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var oldCharacter = claim.Character;
        var target = mock.CreateCharacter("Петя");
        mock.ReInitProjectInfo();

        await CreateService().MoveByMaster(claim.GetId(), "переносим", target.GetId());

        claim.Character.ShouldBe(target);
        target.ApprovedClaim.ShouldBeNull();
        oldCharacter.ApprovedClaim.ShouldBeNull();
        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(1);
    }

    /// <summary>
    /// Персонаж уже занят другой утверждённой заявкой — переносить туда нельзя, и мастер это
    /// обойти не может.
    /// </summary>
    [Fact]
    public async Task MoveByMaster_ToBusyCharacter_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var target = mock.CreateCharacter("Петя");
        _ = mock.CreateApprovedClaim(target, mock.Master);
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<ClaimTargetIsNotAcceptingClaims>(
            () => CreateService().MoveByMaster(claim.GetId(), "переносим", target.GetId()));

        claim.Character.ShouldNotBe(target);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task MoveByMaster_ByPlayer_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var target = mock.CreateCharacter("Петя");
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).MoveByMaster(claim.GetId(), "переносим", target.GetId()));

        claim.Character.ShouldNotBe(target);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    #endregion

    #region CheckInClaim

    private Claim CreateApprovedClaimForCheckIn()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        mock.ReInitProjectInfo();
        return claim;
    }

    [Fact]
    public async Task CheckInClaim_WithoutMoney_ChecksIn_SavesOnce_AndNotifiesOnce()
    {
        var claim = CreateApprovedClaimForCheckIn();

        await CreateService().CheckInClaim(claim.GetId(), money: 0);

        claim.ClaimStatus.ShouldBe(ClaimStatus.CheckedIn);
        claim.CheckInDate.ShouldNotBeNull();
        claim.Character.InGame.ShouldBeTrue();

        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.ShouldHaveSingleItem()
            .ShouldBeOfType<ClaimSimpleChangedNotification>()
            .CommentExtraAction.ShouldBe(CommentExtraAction.CheckedIn);
    }

    /// <summary>
    /// С деньгами уведомлений два, и порядок значим: сначала статусное, потом финансовое. Так было
    /// до миграции, и очередь комментариев контекста обязана этот порядок сохранить.
    /// </summary>
    [Fact]
    public async Task CheckInClaim_WithMoney_SendsStatusNotificationBeforeFinanceOne()
    {
        _ = mock.CreateCashPaymentType();
        var claim = CreateApprovedClaimForCheckIn();

        await CreateService().CheckInClaim(claim.GetId(), money: 100);

        claim.ClaimStatus.ShouldBe(ClaimStatus.CheckedIn);
        claim.FinanceOperations.ShouldHaveSingleItem().MoneyAmount.ShouldBe(100);

        SaveChangesCallCount.ShouldBe(1);
        SentNotifications.Count.ShouldBe(2);
        SentNotifications[0].ShouldBeOfType<ClaimSimpleChangedNotification>()
            .CommentExtraAction.ShouldBe(CommentExtraAction.CheckedIn);
        SentNotifications[1].ShouldBeOfType<ClaimSimpleChangedNotification>()
            .CommentExtraAction.ShouldBe(CommentExtraAction.PaidFee);
    }

    /// <summary>
    /// Наличных денег принять некому — у мастера нет наличного типа оплаты.
    /// </summary>
    [Fact]
    public async Task CheckInClaim_WithMoneyWithoutCashPaymentType_Throws_AndDoesNotSave()
    {
        var claim = CreateApprovedClaimForCheckIn();

        _ = await Should.ThrowAsync<JoinRpgInvalidUserException>(
            () => CreateService().CheckInClaim(claim.GetId(), money: 100));

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task CheckInClaim_WithNegativeMoney_Throws_AndDoesNotSave()
    {
        var claim = CreateApprovedClaimForCheckIn();

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateService().CheckInClaim(claim.GetId(), money: -100));

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.Character.InGame.ShouldBeFalse();
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task CheckInClaim_OfNotApprovedClaim_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        _ = await Should.ThrowAsync<ClaimWrongStatusException>(
            () => CreateService().CheckInClaim(claim.GetId(), money: 0));

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task CheckInClaim_ByPlayer_Throws_AndDoesNotSave()
    {
        var claim = CreateApprovedClaimForCheckIn();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).CheckInClaim(claim.GetId(), money: 0));

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    #endregion

    #region Отзыв приглашений на совместное проживание

    /// <summary>
    /// Приглашения отклоняются в том же единственном сохранении, что и сама заявка. До миграции их
    /// отзыв шёл на собственном <c>DbContext</c> со своим <c>SaveChanges</c> — то есть коммитился
    /// независимо от внешней операции (ADR014).
    /// </summary>
    [Fact]
    public async Task DeclineByMaster_WithInvites_DeclinesThemInSameSave_AndMailsAfterNotification()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var neighbourClaim = CreateClaim(ClaimStatus.AddedByUser, "Сосед");
        var invite = mock.CreateAccommodationInvite(claim, neighbourClaim);

        var invitesDeclinedAtSave = false;
        OnSaveChanges = _ => invitesDeclinedAtSave = invite.IsAccepted == InviteState.Declined;

        await CreateService().DeclineByMaster(
            claim.GetId(), ClaimDenialReason.Refused, "отказ", deleteCharacter: false);

        invite.IsAccepted.ShouldBe(InviteState.Declined);
        invite.ResolveDescription.ShouldBe(ResolveDescription.ClaimCanceled);

        // Главное: отзыв приглашений попал в то же сохранение, а не в своё собственное.
        invitesDeclinedAtSave.ShouldBeTrue();
        SaveChangesCallCount.ShouldBe(1);

        // Письмо о снятии приглашения уходит легаси-каналом, то есть после уведомления.
        var email = SentEmails.ShouldHaveSingleItem().ShouldBeOfType<DeclineInviteEmail>();
        email.RecipientClaims.ShouldBe([neighbourClaim]);
        email.Initiator.ShouldBe(mock.Master);

        SentInOrder.Count.ShouldBe(2);
        _ = SentInOrder[0].ShouldBeOfType<ClaimSimpleChangedNotification>();
        _ = SentInOrder[1].ShouldBeOfType<DeclineInviteEmail>();
    }

    [Fact]
    public async Task DeclineByPlayer_WithInvites_DeclinesThem()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var neighbourClaim = CreateClaim(ClaimStatus.AddedByUser, "Сосед");

        // Приглашение, полученное отклоняемой заявкой, снимается так же, как и отправленное ею.
        var invite = mock.CreateAccommodationInvite(neighbourClaim, claim);

        await CreateService(mock.Player.UserId).DeclineByPlayer(claim.GetId(), "передумал");

        invite.IsAccepted.ShouldBe(InviteState.Declined);
        SaveChangesCallCount.ShouldBe(1);
        SentEmails.ShouldHaveSingleItem().ShouldBeOfType<DeclineInviteEmail>()
            .RecipientClaims.ShouldBe([neighbourClaim]);
    }

    /// <summary>Приглашений нет — писем тоже нет, и лишних запросов не понадобилось.</summary>
    [Fact]
    public async Task DeclineByMaster_WithoutInvites_SendsNoInviteEmail()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await CreateService().DeclineByMaster(
            claim.GetId(), ClaimDenialReason.Refused, "отказ", deleteCharacter: false);

        SentEmails.ShouldBeEmpty();
    }

    #endregion

    #region SetAccommodationType

    [Fact]
    public async Task SetAccommodationType_ToNewType_CreatesRequest_SavesOnce()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var newType = mock.CreateAccommodationType("Домик");

        var request = await CreateService().SetAccommodationType(
            ProjectId.Value, claim.ClaimId, newType.Id);

        request.AccommodationTypeId.ShouldBe(newType.Id);
        request.IsAccepted.ShouldBe(InviteState.Accepted);
        request.Subjects.ShouldBe([claim]);
        claim.AccommodationRequest.ShouldBe(request);

        SaveChangesCallCount.ShouldBe(1);
        SentEmails.ShouldBeEmpty();
    }

    /// <summary>
    /// Смена типа у уже поселённой заявки: старая комната покидается, письмо о выезде уходит
    /// легаси-каналом — то есть после сохранения.
    /// </summary>
    [Fact]
    public async Task SetAccommodationType_WhenAlreadyInRoom_LeavesOldRoom_AndMailsAfterSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var oldRequest = CreateAccommodation(claim);
        var newType = mock.CreateAccommodationType("Домик");

        var savesWhenEmailSent = -1;
        emailService.OnEmail = () => savesWhenEmailSent = SaveChangesCallCount;

        var request = await CreateService().SetAccommodationType(
            ProjectId.Value, claim.ClaimId, newType.Id);

        request.ShouldNotBe(oldRequest);
        oldRequest.Subjects.ShouldBeEmpty();

        SaveChangesCallCount.ShouldBe(1);
        _ = SentEmails.ShouldHaveSingleItem().ShouldBeOfType<LeaveRoomEmail>();
        savesWhenEmailSent.ShouldBe(1);
    }

    /// <summary>Тип уже такой — ранний выход обязан остаться холостым.</summary>
    [Fact]
    public async Task SetAccommodationType_ToSameType_DoesNothing()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var request = CreateAccommodation(claim);

        var result = await CreateService().SetAccommodationType(
            ProjectId.Value, claim.ClaimId, request.AccommodationTypeId);

        result.ShouldBe(request);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
        SentEmails.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetAccommodationType_ToUnknownType_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => CreateService().SetAccommodationType(ProjectId.Value, claim.ClaimId, 12345));

        SaveChangesCallCount.ShouldBe(0);
        SentEmails.ShouldBeEmpty();
    }

    /// <summary>
    /// <see cref="ClaimAccessRequirement.AccommodationChange"/> в действии: у неутверждённой заявки
    /// менять поселение может только обладатель <c>CanSetPlayersAccommodations</c>, у утверждённой —
    /// ещё и сам игрок.
    /// </summary>
    [Fact]
    public async Task SetAccommodationType_OfNotApprovedClaim_ByPlayer_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var newType = mock.CreateAccommodationType("Домик");

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).SetAccommodationType(
                ProjectId.Value, claim.ClaimId, newType.Id));

        claim.AccommodationRequest.ShouldBeNull();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task SetAccommodationType_OfApprovedClaim_ByPlayer_Works()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        mock.ReInitProjectInfo();
        var newType = mock.CreateAccommodationType("Домик");

        var request = await CreateService(mock.Player.UserId).SetAccommodationType(
            ProjectId.Value, claim.ClaimId, newType.Id);

        request.AccommodationTypeId.ShouldBe(newType.Id);
        SaveChangesCallCount.ShouldBe(1);
    }

    #endregion

    #region LeaveAccommodationGroupAsync

    [Fact]
    public async Task LeaveAccommodationGroup_WithoutAccommodation_DoesNothing()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        var result = await CreateService().LeaveAccommodationGroupAsync(ProjectId.Value, claim.ClaimId);

        result.ShouldBeNull();
        SaveChangesCallCount.ShouldBe(0);
        SentEmails.ShouldBeEmpty();
        SentNotifications.ShouldBeEmpty();
    }

    /// <summary>
    /// Заявка живёт в номере одна — выходить не из чего, и это тоже холостой ранний выход.
    /// </summary>
    [Fact]
    public async Task LeaveAccommodationGroup_WhenSoleDweller_DoesNothing()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var request = CreateAccommodation(claim);

        var result = await CreateService().LeaveAccommodationGroupAsync(ProjectId.Value, claim.ClaimId);

        result.ShouldBe(request);
        request.Subjects.ShouldBe([claim]);
        SaveChangesCallCount.ShouldBe(0);
        SentEmails.ShouldBeEmpty();
    }

    [Fact]
    public async Task LeaveAccommodationGroup_FromSharedRoom_MovesToOwnRequest()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        var neighbourClaim = CreateClaim(ClaimStatus.AddedByUser, "Сосед");
        var accommodationType = mock.CreateAccommodationType();
        var request = mock.CreateAccommodationRequest(accommodationType, claim, neighbourClaim);
        _ = mock.CreateRoom(request);

        var result = await CreateService().LeaveAccommodationGroupAsync(ProjectId.Value, claim.ClaimId);

        // Возвращается СТАРАЯ заявка на поселение — так было и до миграции.
        result.ShouldBe(request);
        request.Subjects.ShouldBe([neighbourClaim]);

        // Ушедший получил собственную одноместную заявку того же типа.
        var ownRequest = claim.AccommodationRequest.ShouldNotBeNull();
        ownRequest.ShouldNotBe(request);
        ownRequest.AccommodationTypeId.ShouldBe(accommodationType.Id);
        ownRequest.IsAccepted.ShouldBe(InviteState.Accepted);
        ownRequest.Subjects.ShouldBe([claim]);

        SaveChangesCallCount.ShouldBe(1);
        _ = SentEmails.ShouldHaveSingleItem().ShouldBeOfType<LeaveRoomEmail>();
    }

    [Fact]
    public async Task LeaveAccommodationGroup_OfNotApprovedClaim_ByPlayer_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);
        _ = CreateAccommodation(claim);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId)
                .LeaveAccommodationGroupAsync(ProjectId.Value, claim.ClaimId));

        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion

    #region SaveFieldsFromClaim

    [Fact]
    public async Task SaveFieldsFromClaim_SavesOnce_AndSendsNothing()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await CreateService().SaveFieldsFromClaim(
            claim.GetId(), FieldLayerContainer.Empty(mock.ProjectInfo));

        SaveChangesCallCount.ShouldBe(1);

        // Отправка письма об изменении полей была закомментирована и до миграции — см. ADR014.
        SentNotifications.ShouldBeEmpty();
        SentEmails.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveFieldsFromClaim_ByStranger_Throws_AndDoesNotSave()
    {
        var claim = CreateClaimOfAnotherPlayer();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).SaveFieldsFromClaim(
                claim.GetId(), FieldLayerContainer.Empty(mock.ProjectInfo)));

        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion

    #region MoveToSecondRole

    /// <summary>
    /// Зарегистрированная заявка и свободный персонаж, на которого игрок выходит второй ролью.
    /// </summary>
    private (Claim OldClaim, Character Target) CreateSecondRoleSetup()
    {
        var oldCharacter = mock.CreateCharacter("Вася");
        var oldClaim = mock.CreateCheckedInClaim(oldCharacter, mock.Player);
        oldCharacter.InGame = true;
        var target = mock.CreateCharacter("Петя");
        mock.ReInitProjectInfo();
        return (oldClaim, target);
    }

    /// <summary>Заявка, созданная операцией: это единственная заявка, которой не было до неё.</summary>
    private Claim NewClaimBesides(Claim oldClaim)
        => mock.Project.Claims.Single(claim => claim != oldClaim);

    [Fact]
    public async Task MoveToSecondRole_TakesPlayerOutOfGame_AndCreatesApprovedClaimOnTarget()
    {
        var (oldClaim, target) = CreateSecondRoleSetup();

        _ = await CreateService().MoveToSecondRole(oldClaim.GetId(), target.GetId(), "вторая роль");

        oldClaim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        oldClaim.Character.InGame.ShouldBeFalse();

        var newClaim = NewClaimBesides(oldClaim);
        newClaim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        newClaim.Character.ShouldBe(target);
        newClaim.PlayerUserId.ShouldBe(mock.Player.UserId);
        newClaim.CurrentFee.ShouldBe(0);
        newClaim.MasterAcceptedDate.ShouldNotBeNull();
        target.ApprovedClaim.ShouldBe(newClaim);

        newClaim.CommentDiscussion.Comments.ShouldHaveSingleItem()
            .ExtraAction.ShouldBe(CommentExtraAction.SecondRole);
        oldClaim.CommentDiscussion.Comments.ShouldHaveSingleItem()
            .ExtraAction.ShouldBe(CommentExtraAction.OutOfGame);

        // Путь создания заявки двухфазный: комментарий появляется между сохранениями (ADR014).
        SaveChangesCallCount.ShouldBe(2);
    }

    /// <summary>
    /// Возврат <c>CheckedIn</c> → <c>Approved</c> не должен затирать <c>MasterAcceptedDate</c>: он
    /// виден в отчётах. Поэтому переход идёт через <c>ChangeStatusKeepingTimestamps</c>.
    /// </summary>
    [Fact]
    public async Task MoveToSecondRole_KeepsMasterAcceptedDateOfOldClaim()
    {
        var (oldClaim, target) = CreateSecondRoleSetup();
        var acceptedAt = new DateTime(2020, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        oldClaim.MasterAcceptedDate = acceptedAt;

        _ = await CreateService().MoveToSecondRole(oldClaim.GetId(), target.GetId(), "вторая роль");

        oldClaim.MasterAcceptedDate.ShouldBe(acceptedAt);
    }

    /// <summary>
    /// Уведомление ровно одно — по старой заявке. Комментарий к новой заявке создаётся, но молчит:
    /// так было и до миграции. Слать ли его — открытый вопрос ADR014, и этот тест фиксирует текущий
    /// ответ «не слать».
    /// </summary>
    [Fact]
    public async Task MoveToSecondRole_NotifiesOnlyAboutOldClaim()
    {
        var (oldClaim, target) = CreateSecondRoleSetup();

        _ = await CreateService().MoveToSecondRole(oldClaim.GetId(), target.GetId(), "вторая роль");

        var notification = SentNotifications.ShouldHaveSingleItem()
            .ShouldBeOfType<ClaimSimpleChangedNotification>();
        notification.ClaimId.ShouldBe(oldClaim.GetId());
        notification.CommentExtraAction.ShouldBe(CommentExtraAction.OutOfGame);
        notification.AnotherCharacterId.ShouldBe(target.GetId());
        SentEmails.ShouldBeEmpty();
    }

    /// <summary>
    /// Отметки времени у новой заявки проставляются — до миграции она создавалась мимо
    /// <c>CommentHelper</c> и выглядела «без активности», что искажало индикаторы непрочитанного.
    /// </summary>
    [Fact]
    public async Task MoveToSecondRole_SetsClaimTimesOnNewClaim()
    {
        var (oldClaim, target) = CreateSecondRoleSetup();

        _ = await CreateService().MoveToSecondRole(oldClaim.GetId(), target.GetId(), "вторая роль");

        var newClaim = NewClaimBesides(oldClaim);
        newClaim.LastMasterCommentAt.ShouldNotBeNull();
        newClaim.LastVisibleMasterCommentAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task MoveToSecondRole_OfClaimNotCheckedIn_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim(ClaimStatus.Approved);
        var target = mock.CreateCharacter("Петя");
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<ClaimWrongStatusException>(
            () => CreateService().MoveToSecondRole(claim.GetId(), target.GetId(), "вторая роль"));

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        mock.Project.Claims.ShouldHaveSingleItem().ShouldBe(claim);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task MoveToSecondRole_ToCharacterOfAnotherProject_Throws_AndDoesNotSave()
    {
        var (oldClaim, target) = CreateSecondRoleSetup();
        var alienClaimId = new ClaimIdentification(
            new ProjectIdentification(ProjectId.Value + 1), oldClaim.ClaimId);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateService().MoveToSecondRole(alienClaimId, target.GetId(), "вторая роль"));

        oldClaim.ClaimStatus.ShouldBe(ClaimStatus.CheckedIn);
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    /// <summary>
    /// Права перенесены как есть: нужен любой мастерский доступ (<c>//TODO Specific right</c>).
    /// Игроку операция недоступна.
    /// </summary>
    [Fact]
    public async Task MoveToSecondRole_ByPlayer_Throws_AndDoesNotSave()
    {
        var (oldClaim, target) = CreateSecondRoleSetup();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId)
                .MoveToSecondRole(oldClaim.GetId(), target.GetId(), "вторая роль"));

        oldClaim.ClaimStatus.ShouldBe(ClaimStatus.CheckedIn);
        oldClaim.Character.InGame.ShouldBeTrue();
        SaveChangesCallCount.ShouldBe(0);
        SentNotifications.ShouldBeEmpty();
    }

    #endregion
}
