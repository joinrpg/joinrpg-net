using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Dal.Impl.Repositories;

public class AccommodationRequestRepositoryImpl(MyDbContext ctx) : IAccommodationRequestRepository
{
    public async Task<IReadOnlyCollection<AccommodationRequest>>
        GetAccommodationRequestForProject(int projectId)
    {
        return await ctx.Set<AccommodationRequest>()
            .Where(request => request.ProjectId == projectId)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<AccommodationRequest>>
        GetAccommodationRequestForClaim(int claimId)
    {
        return await ctx.Set<AccommodationRequest>().Where(request =>
                request.Subjects.Any(subject => subject.ClaimId == claimId))
            .Include(request => request.Subjects)
            .Include(request => request.Subjects.Select(cl => cl.Player))
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<Claim>>
        GetClaimsWithSameAccommodationType(int accommodationTypeId)
    {
        return await ctx.Set<AccommodationRequest>().Where(request =>
                request.AccommodationTypeId == accommodationTypeId)
            .SelectMany(request => request.Subjects)
            .Where(claim => claim.ClaimStatus == ClaimStatus.Approved)
            .Include(claim => claim.Player)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<Claim>>
        GetClaimsWithSameAccommodationTypeToInvite(int accommodationTypeId)
    {
        return await ctx.Set<AccommodationRequest>().Where(request =>
                request.AccommodationTypeId == accommodationTypeId &&
                 request.AccommodationId == null)
            .SelectMany(request => request.Subjects)
            .Where(claim => claim.ClaimStatus == ClaimStatus.Approved)
            .Include(claim => claim.Player)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<Claim>> GetClaimsWithSameAccommodationRequest(
        int accommodationRequestId)
    {
        var tmp = await ctx.Set<AccommodationRequest>()
            .Where(request => request.Id == accommodationRequestId)
            .SelectMany(request => request.Subjects)
            .Where(claim => claim.ClaimStatus == ClaimStatus.Approved)
            .Include(claim => claim.Player)
            .ToListAsync().ConfigureAwait(false);
        return tmp;
    }

    public async Task<IEnumerable<Claim>> GetClaimsWithOutAccommodationRequest(
        int projectId)
    {
        var projectClaims = await ctx.Set<Claim>()
            .Where(claim => claim.ProjectId == projectId)
            .Include(claim => claim.Player)
            .Where(claim => claim.ClaimStatus == ClaimStatus.Approved)
            .ToListAsync().ConfigureAwait(false);
        var claimsWithAccommodationRequest = await ctx.Set<AccommodationRequest>()
            .Where(request => request.ProjectId == projectId)
            .SelectMany(request => request.Subjects)
            .Where(claim => claim.ClaimStatus == ClaimStatus.Approved)
            .Include(claim => claim.Player)
            .ToListAsync().ConfigureAwait(false);
        return
            projectClaims.Except(claimsWithAccommodationRequest);
    }
}

