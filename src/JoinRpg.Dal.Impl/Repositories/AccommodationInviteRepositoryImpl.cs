using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Dal.Impl.Repositories;

public class AccommodationInviteRepositoryImpl(MyDbContext ctx) : IAccommodationInviteRepository
{
    public async Task<IEnumerable<AccommodationInvite>>
        GetIncomingInviteForClaim(Claim claim) =>
        await GetIncomingInviteForClaim(claim.ClaimId).ConfigureAwait(false);

    public async Task<IEnumerable<AccommodationInvite>>
        GetIncomingInviteForClaim(int claimId) => await ctx.Set<AccommodationInvite>()
        .Where(invite => invite.ToClaimId == claimId)
        .Include(invite => invite.To.Player)
        .Include(invite => invite.From.Player)
        .ToListAsync().ConfigureAwait(false);

    public async Task<IEnumerable<AccommodationInvite>>
        GetOutgoingInviteForClaim(Claim claim) =>
        await GetOutgoingInviteForClaim(claim.ClaimId).ConfigureAwait(false);

    public async Task<IEnumerable<AccommodationInvite>>
        GetOutgoingInviteForClaim(int claimId) => await ctx.Set<AccommodationInvite>()
        .Where(invite => invite.FromClaimId == claimId)
        .Include(invite => invite.To.Player)
        .Include(invite => invite.From.Player)
        .ToListAsync().ConfigureAwait(false);

    public async Task<AccommodationInviteParticipants> GetInviteParticipants(
        AccommodationInviteIdentification inviteId)
    {
        var inviteIntId = inviteId.AccommodationInviteId;
        var projectIntId = inviteId.ProjectId.Value;

        // Проецируем сразу в идентификаторы: наружу тут нужны только стороны приглашения, а
        // мутировать его будет трекаемая сущность из загрузчика хэндла.
        var participants = await ctx.Set<AccommodationInvite>()
            .Where(invite => invite.Id == inviteIntId && invite.ProjectId == projectIntId)
            .Select(invite => new { invite.FromClaimId, invite.ToClaimId })
            .SingleOrDefaultAsync().ConfigureAwait(false)
            ?? throw new JoinRpgEntityNotFoundException(inviteIntId, nameof(AccommodationInvite));

        return new AccommodationInviteParticipants(
            new ClaimIdentification(inviteId.ProjectId, participants.FromClaimId),
            new ClaimIdentification(inviteId.ProjectId, participants.ToClaimId));
    }
}
