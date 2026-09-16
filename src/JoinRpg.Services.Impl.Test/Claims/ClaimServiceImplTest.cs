using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Interfaces;
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
