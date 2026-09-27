using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.WebPortal.Managers.Accommodation;

namespace JoinRpg.WebPortal.Managers.Test.Accommodation;

/// <summary>
/// Фильтрация типов проживания в диалоге выбора: мастеру видно всё, игроку — только помеченное
/// как выбираемое плюс уже выбранный им тип. После перехода на <c>ProjectInfo</c> (ADR015, PR 3)
/// сервис берёт типы из метаданных проекта, а не из репозитория поселения.
/// </summary>
public class AccommodationTypeViewServiceTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private ClaimIdentification ClaimId => new(Mock.ProjectInfo.ProjectId, Claim.ClaimId);

    private Claim Claim { get; }

    public AccommodationTypeViewServiceTest() => Claim = Mock.CreateClaim(Mock.Character, Mock.Player);

    private AccommodationTypeViewService CreateService(int userId)
        => new(
            new FakeClaimsRepository(Mock),
            new FakeProjectMetadataRepository(Mock),
            // GetAccommodationTypes не обращается к IClaimService — если обратится, тест упадёт,
            // и это ровно то поведение, которое здесь нужно.
            claimService: null!,
            new FakeCurrentUserAccessor(userId));

    private ProjectAccommodationType CreateType(string name, bool isPlayerSelectable)
    {
        var type = Mock.CreateAccommodationType(name, isPlayerSelectable: isPlayerSelectable);
        Mock.ReInitProjectInfo();
        return type;
    }

    [Fact]
    public async Task Master_SeesAllTypes()
    {
        _ = CreateType("Палатка", isPlayerSelectable: true);
        _ = CreateType("Служебный вагончик", isPlayerSelectable: false);

        var result = await CreateService(Mock.Master.UserId).GetAccommodationTypes(ClaimId);

        result.Types.Select(t => t.Name).ShouldBe(["Палатка", "Служебный вагончик"], ignoreOrder: true);
    }

    [Fact]
    public async Task Player_SeesOnlySelectableTypes()
    {
        _ = CreateType("Палатка", isPlayerSelectable: true);
        _ = CreateType("Служебный вагончик", isPlayerSelectable: false);

        var result = await CreateService(Mock.Player.UserId).GetAccommodationTypes(ClaimId);

        result.Types.ShouldHaveSingleItem().Name.ShouldBe("Палатка");
    }

    [Fact]
    public async Task Player_SeesAlreadySelectedTypeEvenIfNotSelectable()
    {
        _ = CreateType("Палатка", isPlayerSelectable: true);
        var serviceType = CreateType("Служебный вагончик", isPlayerSelectable: false);
        _ = Mock.CreateAccommodationRequest(serviceType, Claim);

        var result = await CreateService(Mock.Player.UserId).GetAccommodationTypes(ClaimId);

        result.Types.Select(t => t.Name).ShouldBe(["Палатка", "Служебный вагончик"], ignoreOrder: true);
        result.SelectedTypeId.ShouldBe(new AccommodationTypeIdentification(Mock.ProjectInfo.ProjectId, serviceType.Id));
    }

    [Fact]
    public async Task TypeViewModel_CarriesCostCapacityAndDescription()
    {
        var type = Mock.CreateAccommodationType("Домик", capacity: 3, cost: 1500, isPlayerSelectable: true);
        type.Description = new MarkdownDbValue("Тёплый");
        Mock.ReInitProjectInfo();

        var result = await CreateService(Mock.Master.UserId).GetAccommodationTypes(ClaimId);

        var viewModel = result.Types.ShouldHaveSingleItem();
        viewModel.Capacity.ShouldBe(3);
        viewModel.Cost.ShouldBe(1500);
        viewModel.DescriptionHtml.ShouldContain("Тёплый");
    }

    /// <summary>
    /// Read-репозиторий заявок поверх мока: реализован только <see cref="GetClaim"/>, остальное
    /// бросает намеренно — поход за незапланированными данными должен быть виден в тесте.
    /// </summary>
    private sealed class FakeClaimsRepository(MockedProject mock) : IClaimsRepository
    {
        public Task<Claim?> GetClaim(ClaimIdentification claimId)
            => Task.FromResult(mock.Project.Claims.SingleOrDefault(
                claim => claim.ProjectId == claimId.ProjectId.Value && claim.ClaimId == claimId.ClaimId));

        public void Dispose() { }

        public Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<MyClaimShortInfo>> GetMyActiveClaimsInActiveProjects(UserIdentification userId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(ProjectIdentification projectId, UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<ClaimIdentification> claimIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status) => throw new NotSupportedException();
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
}
