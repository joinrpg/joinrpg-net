using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
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

    // Игрок меняет проживание только у утверждённой заявки — у остальных диалог ему не положен
    public AccommodationTypeViewServiceTest() => Claim = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);

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

    [Theory]
    [InlineData(ClaimStatus.Discussed)]
    [InlineData(ClaimStatus.CheckedIn)]
    public async Task Player_OfNotApprovedClaim_IsDenied(ClaimStatus status)
    {
        // Смена типа игроку на такой заявке откажет, значит и диалог ему не отдаём (#5261)
        Claim.ClaimStatus = status;

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(Mock.Player.UserId).GetAccommodationTypes(ClaimId));
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
}
