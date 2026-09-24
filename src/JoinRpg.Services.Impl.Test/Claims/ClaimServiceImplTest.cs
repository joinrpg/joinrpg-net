using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
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
    /// <summary>Записывает отзыв приглашений на совместное проживание вместо реального.</summary>
    private sealed class FakeAccommodationInviteService : IAccommodationInviteService
    {
        public List<ClaimIdentification> DeclinedInvitesFor { get; } = [];

        public Task DeclineAllClaimInvites(ClaimIdentification claimId)
        {
            DeclinedInvitesFor.Add(claimId);
            return Task.CompletedTask;
        }

        public Task CreateAccommodationInvite(
            ClaimIdentification senderClaimId,
            AccommodationRequestIdentification senderRequestId,
            AccommodationTargetIdentification target) => throw new NotSupportedException();

        public Task<AccommodationInvite?> CancelOrDeclineAccommodationInvite(
            AccommodationInviteIdentification inviteId,
            InviteState newState) => throw new NotSupportedException();

        public Task<AccommodationInvite?> AcceptAccommodationInvite(AccommodationInviteIdentification inviteId)
            => throw new NotSupportedException();
    }

    private readonly FakeAccommodationInviteService accommodationInvites = new();

    private ClaimServiceImpl CreateService(int? currentUserId = null)
    {
        var currentUser = CreateCurrentUser(currentUserId);
        return new ClaimServiceImpl(
            unitOfWork,
            emailService,
            CreateFieldSaveHelper(),
            accommodationInvites,
            currentUser,
            metadataRepository,
            characterInfoRepository: null!,
            claimValidator: null!,
            NullLogger<CharacterServiceImpl>.Instance,
            claimNotifications,
            new CommentHelper(currentUser),
            impersonateAccessor: null!,
            CreatePropsService(currentUserId));
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
        accommodationInvites.DeclinedInvitesFor.ShouldBe([claim.GetId()]);
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
        var room = new ProjectAccommodation
        {
            Id = 1,
            Name = "Комната",
            Project = mock.Project,
            ProjectId = mock.Project.ProjectId,
            Inhabitants = [],
        };

        var request = new AccommodationRequest
        {
            Id = 1,
            Project = mock.Project,
            ProjectId = mock.Project.ProjectId,
            Subjects = [claim],
            Accommodation = room,
            AccommodationId = room.Id,
        };
        room.Inhabitants.Add(request);

        // Подписки собираются обходом заявка → персонаж → группы; в моке коллекции не заведены.
        claim.Subscriptions = [];
        claim.Character.Subscriptions = [];
        foreach (var group in mock.Project.CharacterGroups)
        {
            group.Subscriptions ??= [];
        }

        claim.AccommodationRequest = request;
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
        accommodationInvites.DeclinedInvitesFor.ShouldBe([claim.GetId()]);
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

    /// <summary>
    /// Удаление персонажа при отклонении заявки — мягкое, и связи с сюжетами оно рвать не должно:
    /// <c>RestoreByMaster</c> умеет вернуть заявку, а оборванные связи не вернулись бы.
    /// </summary>
    [Fact]
    public async Task DeclineByMaster_DeletingCharacter_KeepsPlotLinks()
    {
        var character = mock.CreateCharacter("Вася");
        var plot = new PlotElement
        {
            PlotElementId = 1,
            ProjectId = mock.Project.ProjectId,
            Project = mock.Project,
            TargetCharacters = [character],
        };
        character.DirectlyRelatedPlotElements = [plot];
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        mock.ReInitProjectInfo();

        await CreateService().DeclineByMaster(
            claim.GetId(), ClaimDenialReason.Removed, "удаляем", deleteCharacter: true);

        character.IsActive.ShouldBeFalse();
        character.DirectlyRelatedPlotElements.ShouldContain(plot);
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
}
