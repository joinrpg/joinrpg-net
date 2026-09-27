using JoinRpg.DomainTypes.Characters.Claims;

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

    /// <remarks>
    /// Тип проживания и номер нужны для <c>GetRoomFreeSpace()</c>, поэтому инклюдятся здесь:
    /// иначе EF6 догружает их лениво на каждое открытие страницы заявки (#4964).
    /// </remarks>
    public async Task<IReadOnlyCollection<AccommodationRequest>>
        GetAccommodationRequestForClaim(int claimId)
    {
        return await ctx.Set<AccommodationRequest>().Where(request =>
                request.Subjects.Any(subject => subject.ClaimId == claimId))
            .Include(request => request.Subjects)
            .Include(request => request.Subjects.Select(cl => cl.Player))
            .Include(request => request.AccommodationType)
            .Include(request => request.Accommodation.ProjectAccommodationType)
            .Include(request => request.Accommodation.Inhabitants.Select(inhabitant => inhabitant.Subjects))
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

    public async Task<IReadOnlyCollection<AccommodationNeighbourCandidate>>
        GetClaimsWithSameAccommodationTypeToInvite(ProjectIdentification projectId, int accommodationTypeId)
        => await ToCandidates(
            ctx.Set<AccommodationRequest>()
                .Where(request =>
                    request.ProjectId == projectId.Value &&
                    request.AccommodationTypeId == accommodationTypeId &&
                    request.AccommodationId == null)
                .SelectMany(request => request.Subjects),
            projectId);

    public async Task<IReadOnlyCollection<ClaimIdentification>> GetClaimsWithSameAccommodationRequest(
        ProjectIdentification projectId,
        int accommodationRequestId)
    {
        var claimIds = await ctx.Set<Claim>()
            .Where(claim =>
                claim.ProjectId == projectId.Value &&
                claim.AccommodationRequest_Id == accommodationRequestId &&
                claim.ClaimStatus == ClaimStatus.Approved)
            .Select(claim => claim.ClaimId)
            .ToListAsync().ConfigureAwait(false);
        return [.. claimIds.Select(claimId => new ClaimIdentification(projectId, claimId))];
    }

    /// <remarks>
    /// Раньше это были два полных чтения утверждённых заявок проекта и <c>Except</c> в памяти.
    /// Отсутствие заявки на проживание — это ровно <c>AccommodationRequest_Id == null</c>.
    /// </remarks>
    public async Task<IReadOnlyCollection<AccommodationNeighbourCandidate>>
        GetClaimsWithOutAccommodationRequest(ProjectIdentification projectId)
        => await ToCandidates(
            ctx.Set<Claim>().Where(claim =>
                claim.ProjectId == projectId.Value &&
                claim.AccommodationRequest_Id == null),
            projectId);

    /// <summary>
    /// Утверждённые заявки из <paramref name="claims"/> — в строки списка приглашения.
    /// Проекция, а не сущности: см. <see cref="AccommodationNeighbourCandidate"/>.
    /// </summary>
    private static async Task<IReadOnlyCollection<AccommodationNeighbourCandidate>> ToCandidates(
        IQueryable<Claim> claims,
        ProjectIdentification projectId)
    {
        var rows = await claims
            .Where(claim => claim.ClaimStatus == ClaimStatus.Approved)
            .Select(claim => new
            {
                claim.ClaimId,
                claim.AccommodationRequest_Id,
                claim.Character.CharacterName,
                claim.Player.PrefferedName,
                claim.Player.BornName,
                claim.Player.SurName,
                claim.Player.FatherName,
                claim.Player.Email,
            })
            .ToListAsync().ConfigureAwait(false);

        return
        [
            .. rows.Select(row => new AccommodationNeighbourCandidate(
                new ClaimIdentification(projectId, row.ClaimId),
                row.AccommodationRequest_Id,
                new UserDisplayName(
                    new UserFullName(
                        PrefferedName.FromOptional(row.PrefferedName),
                        BornName.FromOptional(row.BornName),
                        SurName.FromOptional(row.SurName),
                        FatherName.FromOptional(row.FatherName)),
                    new Email(row.Email)),
                row.CharacterName)),
        ];
    }
}
