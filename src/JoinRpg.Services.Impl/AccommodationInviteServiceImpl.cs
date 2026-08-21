using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

public class AccommodationInviteServiceImpl : DbServiceImplBase, IAccommodationInviteService
{
    public AccommodationInviteServiceImpl(IUnitOfWork unitOfWork, IEmailService emailService, ICurrentUserAccessor currentUserAccessor) :
        base(unitOfWork, currentUserAccessor) => EmailService = emailService;

    private IEmailService EmailService { get; }

    /// <inheritdoc />
    public async Task CreateAccommodationInvite(
        ClaimIdentification senderClaimId,
        AccommodationRequestIdentification senderRequestId,
        AccommodationTargetIdentification target)
    {
        // TODO: Search for and reuse previously cancelled invitation(s) to the same person(s)

        _ = new IProjectEntityId[] { senderRequestId, target }.EnsureProject(senderClaimId.ProjectId);

        // Приглашать может либо сам игрок, либо мастер с правом расселять — как и в остальных
        // операциях с проживанием (см. IClaimService.SetAccommodationType/LeaveAccommodationGroupAsync)
        var senderClaim = await ClaimsRepository.GetClaim(senderClaimId).ConfigureAwait(false);
        _ = senderClaim.RequestAccess(currentUserAccessor.UserIdentification,
            Permission.CanSetPlayersAccommodations,
            senderClaim?.ClaimStatus == ClaimStatus.Approved
                ? ExtraAccessReason.PlayerOrResponsible
                : ExtraAccessReason.None);

        if (target.AsAccommodationRequestId() is { } receiverRequestId)
        {
            await CreateAccommodationInviteToAccommodationRequest(senderClaimId, senderRequestId, receiverRequestId)
                .ConfigureAwait(false);
        }
        else if (target.AsClaimId() is { } receiverClaimId)
        {
            await CreateAccommodationInviteToClaim(senderClaimId, senderRequestId, receiverClaimId)
                .ConfigureAwait(false);
        }
        else
        {
            //TODO[Localize]
            throw new AccommodationInviteNotAllowedException(target.ProjectId, "Не выбрано, кого приглашать.");
        }
    }

    private async Task CreateAccommodationInviteToClaim(
        ClaimIdentification senderClaimId,
        AccommodationRequestIdentification senderRequestId,
        ClaimIdentification receiverClaimId)
    {
        var receiverCurrentAccommodationRequest = await UnitOfWork
            .GetDbSet<Claim>()
            .Where(claim => claim.ClaimId == receiverClaimId.ClaimId)
            .Select(claim => claim.AccommodationRequest)
            .Include(request => request.Subjects)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        var senderAccommodationRequest = await UnitOfWork.GetDbSet<AccommodationRequest>()
            .Where(request => request.Id == senderRequestId.AccommodationRequestId)
            .Include(request => request.Subjects)
            .Include(request => request.AccommodationType)
            .Include(c => c.Project)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        EnsureCanInvite(
            senderRequestId.ProjectId,
            senderAccommodationRequest,
            receiverCurrentAccommodationRequest,
            newDwellersCount: receiverCurrentAccommodationRequest?.Subjects.Count ?? 1);

        var inviteRequest = new AccommodationInvite
        {
            ProjectId = senderClaimId.ProjectId.Value,
            FromClaimId = senderClaimId.ClaimId,
            ToClaimId = receiverClaimId.ClaimId,
            IsAccepted = InviteState.Unanswered,
        };

        _ = UnitOfWork.GetDbSet<AccommodationInvite>().Add(inviteRequest);
        await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        var receiver = await UnitOfWork
            .GetDbSet<Claim>()
            .Where(claim => claim.ClaimId == receiverClaimId.ClaimId)
            .ToArrayAsync().ConfigureAwait(false);

        await EmailService
            .Email(await CreateInviteEmail<NewInviteEmail>(receiver,
                senderAccommodationRequest.Project).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    private async Task CreateAccommodationInviteToAccommodationRequest(
        ClaimIdentification senderClaimId,
        AccommodationRequestIdentification senderRequestId,
        AccommodationRequestIdentification receiverRequestId)
    {
        var receiverCurrentAccommodationRequest = await UnitOfWork
            .GetDbSet<AccommodationRequest>()
            .Where(request => request.Id == receiverRequestId.AccommodationRequestId)
            .Include(request => request.Subjects)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        var senderAccommodationRequest = await UnitOfWork.GetDbSet<AccommodationRequest>()
            .Where(request => request.Id == senderRequestId.AccommodationRequestId)
            .Include(request => request.Subjects)
            .Include(request => request.AccommodationType)
            .Include(c => c.Project)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        EnsureCanInvite(
            senderRequestId.ProjectId,
            senderAccommodationRequest,
            receiverCurrentAccommodationRequest,
            newDwellersCount: receiverCurrentAccommodationRequest?.Subjects.Count ?? 1);

        var receiversClaims = await UnitOfWork
            .GetDbSet<Claim>()
            .Where(claim => claim.AccommodationRequest_Id == receiverRequestId.AccommodationRequestId)
            .Include(c => c.Player)
            .ToArrayAsync()
            .ConfigureAwait(false);

        foreach (var receiverClaim in receiversClaims)
        {
            _ = UnitOfWork.GetDbSet<AccommodationInvite>().Add(new AccommodationInvite
            {
                ProjectId = senderClaimId.ProjectId.Value,
                FromClaimId = senderClaimId.ClaimId,
                ToClaimId = receiverClaim.ClaimId,
                IsAccepted = InviteState.Unanswered,
            });
        }

        await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        await EmailService
            .Email(await CreateInviteEmail<NewInviteEmail>(receiversClaims,
                senderAccommodationRequest.Project).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Общие для обоих видов приглашения проверки. Раньше каждая из них молча возвращала
    /// <c>null</c>, и игрок не понимал, почему приглашение не отправилось.
    /// </summary>
    //TODO[Localize]
    internal static void EnsureCanInvite(
        ProjectIdentification projectId,
        AccommodationRequest? senderRequest,
        AccommodationRequest? receiverRequest,
        int newDwellersCount)
    {
        if (senderRequest is null)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "У приглашающего не выбран тип проживания.");
        }

        // Приглашать и приглашаться могут только те, кого ещё не расселили по конкретным комнатам
        if (receiverRequest?.AccommodationId != null || senderRequest.AccommodationId != null)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "Нельзя приглашать, когда кто-то из участников уже расселён по комнатам.");
        }

        // Приглашать можно либо в такой же тип проживания, либо тех, кто ещё не выбрал тип
        if (receiverRequest != null && receiverRequest.AccommodationTypeId != senderRequest.AccommodationTypeId)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "Приглашать можно только тех, кто выбрал такой же тип проживания.");
        }

        if (senderRequest.Subjects.Count + newDwellersCount > senderRequest.AccommodationType.Capacity)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "В номере не хватает мест для всех приглашаемых.");
        }
    }

    private async Task<T> CreateInviteEmail<T>(Claim[] recipients, Project project)
        where T : InviteEmailModel, new()
    {
        return new T()
        {
            Initiator = await GetCurrentUser().ConfigureAwait(false),
            ProjectName = project.ProjectName,
            Recipients = recipients.GetInviteSubscriptions(),
            RecipientClaims = recipients,
            Text = new MarkdownDbValue(),
        };
    }

    /// <inheritdoc />
    public async Task<AccommodationInvite?> AcceptAccommodationInvite(AccommodationInviteIdentification inviteId)
    {
        //todo: make null result descriptive
        var inviteRequest = await UnitOfWork.GetDbSet<AccommodationInvite>()
            .Where(invite => invite.Id == inviteId.AccommodationInviteId)
            .Include(invite => invite.To)
            .Include(invite => invite.From)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        var receiverAccommodationRequest =
            await GetAccommodationRequestByClaim(inviteRequest.ToClaimId).ConfigureAwait(false);
        var senderAccommodationRequest =
            await GetAccommodationRequestByClaim(inviteRequest.FromClaimId)
                .ConfigureAwait(false);

        var roomFreeSpace = (senderAccommodationRequest.AccommodationId != null)
            ? senderAccommodationRequest.Accommodation.GetRoomFreeSpace()
            : senderAccommodationRequest.GetAbstractRoomFreeSpace();


        var canInvite = roomFreeSpace >= (receiverAccommodationRequest?.Subjects.Count ?? 0);

        if (!canInvite)
        {
            return null;
        }

        _ = (receiverAccommodationRequest?.Subjects.Remove(inviteRequest.To));
        senderAccommodationRequest.Subjects.Add(inviteRequest.To);
        inviteRequest.To.AccommodationRequest = senderAccommodationRequest;

        if (receiverAccommodationRequest != null)
        {
            foreach (var claim in receiverAccommodationRequest.Subjects.ToList())
            {
                await DeclineOtherInvite(claim.ClaimId, inviteId.AccommodationInviteId).ConfigureAwait(false);
                senderAccommodationRequest.Subjects.Add(claim);
            }

            _ = UnitOfWork.GetDbSet<AccommodationRequest>().Remove(receiverAccommodationRequest);
        }

        inviteRequest.IsAccepted = InviteState.Accepted;
        inviteRequest.ResolveDescription = ResolveDescription.Accepted;
        await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);


        var receivers = await UnitOfWork.GetDbSet<Claim>()
            .Where(claim => inviteRequest.FromClaimId == claim.ClaimId)
            .Include(claim => claim.Player)
            .ToArrayAsync()
            .ConfigureAwait(false);

        await EmailService
            .Email(await CreateInviteEmail<AcceptInviteEmail>(receivers,
                inviteRequest.Project).ConfigureAwait(false))
            .ConfigureAwait(false);


        return inviteRequest;
    }

    public async Task<AccommodationInvite?> CancelOrDeclineAccommodationInvite(
        AccommodationInviteIdentification inviteId,
        InviteState newState)
    {
        var acceptedStates = new[]
        {
            InviteState.Declined, InviteState.Canceled,
        };

        if (!acceptedStates.Contains(newState))
        {
            return null;
        }

        //todo: make null result descriptive
        var inviteRequest = await UnitOfWork.GetDbSet<AccommodationInvite>()
            .Where(invite => invite.Id == inviteId.AccommodationInviteId)
            .Include(invite => invite.Project)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        if (inviteRequest == null)
        {
            throw new Exception("Invite request not found.");
        }

        inviteRequest.IsAccepted = newState;
        inviteRequest.ResolveDescription = newState == InviteState.Canceled
            ? ResolveDescription.Canceled
            : ResolveDescription.Declined;
        await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        var receivers = await UnitOfWork
            .GetDbSet<Claim>()
            .Where(claim =>
                claim.ClaimId == inviteRequest.FromClaimId ||
                claim.ClaimId == inviteRequest.ToClaimId)
            .Include(c => c.Player)
            .ToArrayAsync()
            .ConfigureAwait(false);

        await EmailService
            .Email(await CreateInviteEmail<DeclineInviteEmail>(receivers,
                inviteRequest.Project).ConfigureAwait(false))
            .ConfigureAwait(false);

        return inviteRequest;
    }

    public async Task DeclineAllClaimInvites(ClaimIdentification claimId)
    {
        var inviteRequests = await UnitOfWork.GetDbSet<AccommodationInvite>()
            .Where(invite => invite.ToClaimId == claimId.ClaimId || invite.FromClaimId == claimId.ClaimId)
            .ToListAsync()
            .ConfigureAwait(false);

        if (inviteRequests.Count == 0)
        {
            return;
        }

        var claims = new List<int>();
        foreach (var accommodationInvite in inviteRequests)
        {
            claims.Add(accommodationInvite.FromClaimId);
            claims.Add(accommodationInvite.ToClaimId);
            accommodationInvite.IsAccepted = InviteState.Declined;
            accommodationInvite.ResolveDescription = ResolveDescription.ClaimCanceled;
        }

        await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        claims = claims.Distinct().ToList();
        _ = claims.Remove(claimId.ClaimId);

        var receivers = await UnitOfWork
            .GetDbSet<Claim>()
            .Where(claim => claims.Contains(claim.ClaimId))
            .Include(c => c.Player)
            .ToArrayAsync()
            .ConfigureAwait(false);

        var firstClaim = receivers.First();
        var project = await UnitOfWork.GetDbSet<Project>()
            .Where(proj => proj.ProjectId == firstClaim.ProjectId)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        await EmailService
            .Email(await CreateInviteEmail<DeclineInviteEmail>(receivers,
                project).ConfigureAwait(false))
            .ConfigureAwait(false);
    }


    private async Task<AccommodationRequest> GetAccommodationRequestByClaim(int claimId) =>
        await UnitOfWork.GetDbSet<AccommodationRequest>()
            .Where(request => request.Subjects.Any(subject => subject.ClaimId == claimId))
            .Where(request => request.IsAccepted == InviteState.Accepted)
            .Include(request => request.Subjects)
            .Include(request => request.AccommodationType)
            .Include(request => request.Accommodation)
            .FirstOrDefaultAsync().ConfigureAwait(false);


    private async Task DeclineOtherInvite(int claimId,
        int inviteId)
    {
        var inviteRequests = await UnitOfWork.GetDbSet<AccommodationInvite>()
            .Where(invite => invite.ToClaimId == claimId)
            .Where(invite => invite.Id != inviteId)
            .ToListAsync().ConfigureAwait(false);
        var stateToDecline = new[] { InviteState.Unanswered };
        foreach (var accommodationInvite in inviteRequests)
        {
            if (!stateToDecline.Contains(accommodationInvite.IsAccepted))
            {
                continue;
            }

            accommodationInvite.IsAccepted = InviteState.Declined;
            accommodationInvite.ResolveDescription = ResolveDescription.DeclinedWithAcceptOther;
        }

        await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);
    }
}
