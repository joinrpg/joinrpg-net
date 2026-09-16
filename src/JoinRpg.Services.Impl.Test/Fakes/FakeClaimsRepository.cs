using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Read-репозиторий заявок поверх <see cref="MockedProject"/>. Нужен, например, автоприёму, который
/// перечитывает заявку заново, а не смотрит в мутированный граф предыдущей операции (ADR014, §7).
/// </summary>
/// <remarks>
/// Реализованы ровно те методы, которые нужны проверяемым операциям; остальные бросают
/// <see cref="NotSupportedException"/> намеренно — чтобы поход за незапланированными данными был
/// виден в тесте, а не подменялся пустышкой.
/// </remarks>
internal sealed class FakeClaimsRepository(MockedProject? mock = null) : IClaimsRepository
{
    private MockedProject Mock => mock ?? throw new NotSupportedException("Фейк создан без MockedProject");

    /// <summary>Заранее заготовленные заявки по (ProjectId, UserId) ответственного мастера.</summary>
    public Dictionary<(int ProjectId, int UserId), List<Claim>> ClaimsByResponsibleMaster { get; } = [];

    public Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status)
        => Task.FromResult<IReadOnlyCollection<Claim>>(
            ClaimsByResponsibleMaster.TryGetValue((projectId, userId), out var claims) ? claims : []);

    public Task<Claim?> GetClaim(ClaimIdentification claimId)
        => Task.FromResult(Mock.Project.Claims.SingleOrDefault(
            claim => claim.ProjectId == claimId.ProjectId.Value && claim.ClaimId == claimId.ClaimId));

    public void Dispose() { }

    public Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(ProjectIdentification projectId, UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<ClaimIdentification> claimIds) => throw new NotSupportedException();
    public Task<Claim?> GetClaimWithDetails(ClaimIdentification claimId) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForGroups(ProjectIdentification projectId, ClaimStatusSpec active, CharacterGroupIdentification[] characterGroupsIds) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<CharacterGroupIdentification> characterGroupsIds, ClaimStatusSpec spec) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimsHeadersForPlayer(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec, UserIdentification userId) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<ClaimCountByMaster>> GetClaimsCountByMasters(int projectId, ClaimStatusSpec claimStatusSpec) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(ProjectIdentification projectId, ClaimStatusSpec approved) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForRoomType(int projectId, ClaimStatusSpec claimStatusSpec, int? roomTypeId) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForMoneyTransfersListAsync(int projectId, ClaimStatusSpec claimStatusSpec) => throw new NotSupportedException();
    public Task<Dictionary<int, int>> GetUnreadDiscussionsForClaims(int projectId, ClaimStatusSpec claimStatusSpec, int userId, bool hasMasterAccess) => throw new NotSupportedException();
}
