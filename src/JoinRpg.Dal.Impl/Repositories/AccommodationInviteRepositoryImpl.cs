using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories;

public class AccommodationInviteRepositoryImpl(MyDbContext ctx) : RepositoryImplBase(ctx),
    IAccommodationInviteRepository
{
    public async Task<IEnumerable<AccommodationInvite>>
        GetIncomingInviteForClaim(Claim claim) =>
        await GetIncomingInviteForClaim(claim.ClaimId).ConfigureAwait(false);

    public async Task<IEnumerable<AccommodationInvite>>
        GetIncomingInviteForClaim(int claimId) => await Ctx.Set<AccommodationInvite>()
        .Where(invite => invite.ToClaimId == claimId)
        .Include(invite => invite.To.Player)
        .Include(invite => invite.From.Player)
        .ToListAsync().ConfigureAwait(false);

    public async Task<IEnumerable<AccommodationInvite>>
        GetOutgoingInviteForClaim(Claim claim) =>
        await GetOutgoingInviteForClaim(claim.ClaimId).ConfigureAwait(false);

    public async Task<IEnumerable<AccommodationInvite>>
        GetOutgoingInviteForClaim(int claimId) => await Ctx.Set<AccommodationInvite>()
        .Where(invite => invite.FromClaimId == claimId)
        .Include(invite => invite.To.Player)
        .Include(invite => invite.From.Player)
        .ToListAsync().ConfigureAwait(false);
}
