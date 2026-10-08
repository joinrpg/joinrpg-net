using JoinRpg.Data.Interfaces;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Read-репозиторий групп проживающих поверх <see cref="MockedProject"/>.
/// </summary>
/// <remarks>
/// Реализованы ровно те методы, которые нужны проверяемым операциям; остальные бросают
/// <see cref="NotSupportedException"/> намеренно — как и в <see cref="FakeClaimsRepository"/>.
/// </remarks>
public sealed class FakeAccommodationRequestRepository(MockedProject mock) : IAccommodationRequestRepository
{
    public Task<IReadOnlyCollection<AccommodationRequest>> GetAccommodationRequestForClaim(int claimId)
        => Task.FromResult<IReadOnlyCollection<AccommodationRequest>>(
            [.. mock.AccommodationRequests.Where(request => request.Subjects.Any(claim => claim.ClaimId == claimId))]);

    public Task<IReadOnlyCollection<Claim>> GetClaimsWithSameAccommodationTypeToInvite(int accommodationTypeId) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsWithSameAccommodationRequest(int accommodationRequestId) => throw new NotSupportedException();
    public Task<IEnumerable<Claim>> GetClaimsWithOutAccommodationRequest(int projectId) => throw new NotSupportedException();
}
