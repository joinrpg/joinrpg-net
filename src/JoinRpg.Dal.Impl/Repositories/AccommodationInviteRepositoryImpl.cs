using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using Microsoft.EntityFrameworkCore;

namespace JoinRpg.Dal.Impl.Repositories;

[UsedImplicitly]
public class AccommodationInviteRepositoryImpl : RepositoryImplBase,
    IAccommodationInviteRepository
{
    public AccommodationInviteRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }

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
