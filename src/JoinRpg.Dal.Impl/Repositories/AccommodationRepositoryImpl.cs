using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

public class AccommodationRepositoryImpl : RepositoryImplBase, IAccommodationRepository
{
    public AccommodationRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }

    public async Task<IReadOnlyCollection<ProjectAccommodationType>> GetAccommodationForProject(
        int projectId)
    {
        return await Ctx.Set<ProjectAccommodationType>().Where(a => a.ProjectId == projectId)
            .Include(x => x.ProjectAccommodations)
            .ToListAsync()
            .ConfigureAwait(false);
    }


    public async Task<IReadOnlyCollection<ClaimAccommodationInfoRow>>
        GetClaimAccommodationReport(int project)
    {
        return await Ctx.Set<Claim>().AsExpandable().Include(claim => claim.Player.Extra)
            .Where(ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active))
            .Where(claim => claim.ProjectId == project)
            .Select(
                claim => new ClaimAccommodationInfoRow()
                {
                    ClaimId = claim.ClaimId,
                    AccomodationType = claim.AccommodationRequest != null
                        ? claim.AccommodationRequest.AccommodationType.Name
                        : null,
                    RoomName =
                        claim.AccommodationRequest != null &&
                        claim.AccommodationRequest.Accommodation != null
                            ? claim.AccommodationRequest.Accommodation.Name
                            : null,
                    User = claim.Player,
                }).ToListAsync();

    }

    public async Task<IReadOnlyCollection<RoomTypeInfoRow>> GetRoomTypesForProject(int project)
    {

        return await Ctx.Set<ProjectAccommodationType>().Where(a => a.ProjectId == project)
            .Include(x => x.Project)
            .Select(x => new RoomTypeInfoRow()
            {
                RoomType = x,
                // cast to int? required to correctly handle SQL-LINQ nullness
                Occupied = x.ProjectAccommodations.Sum(room => room.Inhabitants.Sum(ar => (int?)ar.Subjects.Count)) ?? 0,
                RoomsCount = x.ProjectAccommodations.Count,
                ApprovedClaims = x.Desirous.Sum(ar => (int?)ar.Subjects.Count) ?? 0,
            })
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<ProjectAccommodationType> GetRoomTypeById(int roomTypeId)
    {
        return await Ctx.Set<ProjectAccommodationType>().Where(a => a.Id == roomTypeId)
            .Include(x => x.ProjectAccommodations)
            .SingleAsync()
            .ConfigureAwait(false);
    }
}
