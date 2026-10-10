using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using JoinRpg.Web.Accommodation.Rooms;
using JoinRpg.Web.ProjectCommon.Claims;
using JoinRpg.WebPortal.Managers.Accommodation;

namespace JoinRpg.WebPortal.Managers.Test.Accommodation;

/// <summary>
/// Порядок и деньги на странице «Комнаты» — то, что раньше считала вью-модель страницы на jQuery.
/// </summary>
public class RoomTypeRoomsViewModelBuilderTest
{
    private readonly MockedProject mock = new();
    private readonly Dictionary<ClaimIdentification, RoomResidentWithFee> residents = [];
    private int nextClaimId = 1000;

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private UserIdentification Master => new(mock.Master.UserId);

    private AccommodationTypeInfo CreateType(string name, int capacity, DataModel.ProjectRoomCategory? category = null)
    {
        var type = mock.CreateAccommodationType(name, capacity: capacity, roomCategory: category);
        mock.ReInitProjectInfo();
        return mock.ProjectInfo.AccommodationSettings.GetTypeById(new(ProjectId, type.Id));
    }

    private AccommodationGroupInfo Group(
        AccommodationTypeInfo type,
        int groupId,
        AccommodationRoomIdentification? roomId,
        params (string Name, int FeeTotal, int FeeDue)[] members)
    {
        var claims = new List<ClaimIdentification>();
        foreach (var (name, feeTotal, feeDue) in members)
        {
            var claimId = new ClaimIdentification(ProjectId, nextClaimId++);
            residents[claimId] = new RoomResidentWithFee(
                new ClaimLinkViewModel(claimId, new UserDisplayName(name, null), $"Персонаж {name}", "", new UserIdentification(1)),
                feeTotal,
                feeDue);
            claims.Add(claimId);
        }

        return new AccommodationGroupInfo(new(ProjectId, groupId), type.Id, roomId, claims);
    }

    private AccommodationRoomIdentification RoomId(int id) => new(ProjectId, id);

    private RoomTypeRoomsViewModel Build(
        IReadOnlyCollection<AccommodationTypeInfo> types,
        IReadOnlyCollection<RoomInfo> rooms,
        IReadOnlyCollection<AccommodationGroupInfo> groups)
        => JsonRoundTrip.Ensure(RoomTypeRoomsViewModelBuilder.Build(
            new RoomCategoryPlan(types.First().RoomCategoryId, mock.ProjectInfo, types, rooms, groups),
            types.First().Id,
            residents,
            Master));

    [Fact]
    public void UnassignedAreSortedBySizeThenDebtThenNames()
    {
        var type = CreateType("Шатёр", capacity: 4);
        var pair = Group(type, 1, null, ("Борис", 100, 0), ("Анна", 100, 0));
        var debtor = Group(type, 2, null, ("Вера", 100, 100));
        var payerZ = Group(type, 3, null, ("Яков", 100, 0));
        var payerA = Group(type, 4, null, ("Алла", 100, 0));

        var model = Build([type], [], [pair, debtor, payerZ, payerA]);

        model.UnassignedGroups.Select(g => g.GroupId.AccommodationRequestId).ShouldBe([4, 3, 2, 1]);
    }

    [Fact]
    public void RoomsAreSortedByOccupancyFullLastThenByNumber()
    {
        var type = CreateType("Двушка", capacity: 2);
        var full = Group(type, 1, RoomId(1), ("А", 0, 0), ("Б", 0, 0));
        var half = Group(type, 2, RoomId(2), ("В", 0, 0));
        var rooms = new[]
        {
            new RoomInfo(RoomId(1), "1", [full]),
            new RoomInfo(RoomId(2), "2", [half]),
            new RoomInfo(RoomId(3), "10", []),
            new RoomInfo(RoomId(4), "9", []),
        };

        var model = Build([type], rooms, [full, half]);

        model.Rooms.Select(r => r.Name).ShouldBe(["2", "9", "10", "1"]);
    }

    [Fact]
    public void EqualOccupancy_NumbersFirstThenOtherNames()
    {
        var type = CreateType("Двушка", capacity: 2);
        var rooms = new[] { "1а", "10", "2", "Б" }
            .Select((name, i) => new RoomInfo(RoomId(i + 1), name, []))
            .ToArray();

        var model = Build([type], rooms, []);

        model.Rooms.Select(r => r.Name).ShouldBe(["2", "10", "1а", "Б"]);
    }

    [Fact]
    public void GroupFeesAreSummedAndOverpaymentIsNotDebt()
    {
        var type = CreateType("Шатёр", capacity: 4);
        var debtors = Group(type, 1, null, ("А", 1000, 300), ("Б", 1000, 200));
        var overpaid = Group(type, 2, null, ("В", 1000, -500));

        var model = Build([type], [], [debtors, overpaid]);

        var debtorsModel = model.UnassignedGroups.Single(g => g.GroupId == debtors.Id);
        debtorsModel.FeeTotal.ShouldBe(2000);
        debtorsModel.FeeToPay.ShouldBe(500);
        model.UnassignedGroups.Single(g => g.GroupId == overpaid.Id).FeeToPay.ShouldBe(0);
    }

    [Fact]
    public void GroupCarriesCapacityOfItsOwnType()
    {
        // ADR020: из одной категории селятся несколько типов, и каждая группа ограничивает
        // комнату вместимостью своего типа, а не типа страницы.
        var lux = CreateType("Люкс", capacity: 2);
        var single = CreateType("Люкс на одного", capacity: 1, mock.RoomCategories.Single());
        // ReInitProjectInfo подменил экземпляр метаданных — план требует типы именно из него.
        lux = mock.ProjectInfo.AccommodationSettings.GetTypeById(lux.Id);
        var group = Group(single, 1, null, ("А", 0, 0));

        var model = Build([lux, single], [], [group]);

        model.TypeCapacity.ShouldBe(2);
        model.RoomCapacity.ShouldBe(2);
        var groupModel = model.UnassignedGroups.ShouldHaveSingleItem();
        groupModel.TypeId.ShouldBe(single.Id);
        groupModel.TypeCapacity.ShouldBe(1);
    }

    [Fact]
    public void SharedPool_LabelsGroupsWithTypeAndListsSiblings()
    {
        var lux = CreateType("Люкс", capacity: 2);
        var single = CreateType("Люкс на одного", capacity: 1, mock.RoomCategories.Single());
        lux = mock.ProjectInfo.AccommodationSettings.GetTypeById(lux.Id);
        var own = Group(lux, 1, null, ("А", 0, 0));
        var sibling = Group(single, 2, null, ("Б", 0, 0));

        var model = Build([lux, single], [], [own, sibling]);

        model.SiblingTypeNames.ShouldBe(["Люкс на одного"]);
        model.UnassignedGroups.Single(g => g.GroupId == own.Id).TypeName.ShouldBe("Люкс");
        model.UnassignedGroups.Single(g => g.GroupId == sibling.Id).TypeName.ShouldBe("Люкс на одного");
    }

    [Fact]
    public void SingleTypePool_HasNoTypeLabels()
    {
        var type = CreateType("Двушка", capacity: 2);
        var group = Group(type, 1, null, ("А", 0, 0));

        var model = Build([type], [], [group]);

        model.SiblingTypeNames.ShouldBeEmpty();
        model.UnassignedGroups.ShouldHaveSingleItem().TypeName.ShouldBeNull();
    }

    [Fact]
    public void RoomFilledBySmallerType_IsSortedAsFull()
    {
        // ADR018: «Люкс на одного» делает двухместную комнату полной, и она уходит в конец списка,
        // хотя физически одна койка там свободна.
        var lux = CreateType("Люкс", capacity: 2);
        var single = CreateType("Люкс на одного", capacity: 1, mock.RoomCategories.Single());
        lux = mock.ProjectInfo.AccommodationSettings.GetTypeById(lux.Id);
        var loner = Group(single, 1, RoomId(1), ("А", 0, 0));
        var half = Group(lux, 2, RoomId(2), ("Б", 0, 0));
        var rooms = new[]
        {
            new RoomInfo(RoomId(1), "1", [loner]),
            new RoomInfo(RoomId(2), "2", [half]),
        };

        var model = Build([lux, single], rooms, [loner, half]);

        model.Rooms.Select(r => r.Name).ShouldBe(["2", "1"]);
    }
}
