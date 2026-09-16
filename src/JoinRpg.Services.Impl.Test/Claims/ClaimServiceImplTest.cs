using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
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
}
