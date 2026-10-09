using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Mocks.Fakes;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.Models.Accommodation;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Панель «Проживание» на странице заявки строится из снимка заявки и плана поселения (ADR022).
/// </summary>
public class ClaimAccommodationViewModelTest
{
    private readonly MockedProject mock = new();

    private CharacterClaimInfo ClaimInfo(Claim claim)
        => mock.GetCharacterInfo(claim.Character).Claims.Single(c => c.ClaimId == claim.GetId());

    private async Task<ClaimAccommodationViewModel> Build(Claim claim, params UserInfoHeader[] neighbours)
    {
        var info = ClaimInfo(claim);
        var plan = info.AccommodationTypeId is { } typeId
            ? await new FakeRoomCategoryPlanRepository(mock).GetPlanForTypeOrDefault(typeId)
            : null;
        return new ClaimAccommodationViewModel(info, mock.ProjectInfo, plan, neighbours);
    }

    [Fact]
    public async Task WithoutAccommodation_NothingSelected()
    {
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Одиночка"), mock.Player);

        var model = await Build(claim);

        model.AccommodationType.ShouldBeNull();
        model.RoomName.ShouldBeNull();
        model.RoomFreeSpace.ShouldBe(0);
        model.Neighbours.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnplacedGroup_TypeFromMetadata_FreeSpaceByType()
    {
        var type = mock.CreateAccommodationType("Домик", capacity: 3);
        mock.ReInitProjectInfo();
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Жилец"), mock.Player);
        _ = mock.CreateAccommodationRequest(type, claim);

        var model = await Build(claim);

        model.AccommodationType.ShouldNotBeNull().Name.ShouldBe("Домик");
        model.RoomName.ShouldBeNull();
        model.RoomFreeSpace.ShouldBe(2);
    }

    [Fact]
    public async Task PlacedGroup_RoomNameAndNeighbours()
    {
        var type = mock.CreateAccommodationType("Домик", capacity: 3);
        mock.ReInitProjectInfo();
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Жилец"), mock.Player);
        var neighbour = mock.CreateApprovedClaim(mock.CreateCharacter("Сосед"), mock.Master);
        _ = mock.CreateRoom(mock.CreateAccommodationRequest(type, claim, neighbour), "101");

        var model = await Build(claim, mock.Master.ToUserInfoHeader());

        model.RoomName.ShouldBe("101");
        model.RoomFreeSpace.ShouldBe(1);
        var link = model.Neighbours.ShouldHaveSingleItem();
        link.UserId.ShouldBe(mock.Master.ToUserInfoHeader().UserId);
    }

    [Fact]
    public async Task TwoGroupsInRoom_FreeSpaceByRoom_RoomFull()
    {
        // Свободное место считается по комнате, а не по своей группе: вторая группа её заполнила.
        var lux = mock.CreateAccommodationType("Люкс", capacity: 2);
        mock.ReInitProjectInfo();
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Жилец"), mock.Player);
        var room = mock.CreateRoom(mock.CreateAccommodationRequest(lux, claim), "101");
        var other = mock.CreateAccommodationRequest(lux, mock.CreateApprovedClaim(mock.CreateCharacter("Сосед"), mock.Master));
        room.Inhabitants.Add(other);
        other.Accommodation = room;
        other.AccommodationId = room.Id;

        var model = await Build(claim);

        model.RoomName.ShouldBe("101");
        model.RoomFreeSpace.ShouldBe(0);
    }

    [Fact]
    public void TypeWithoutPlan_IsRejected()
    {
        var type = mock.CreateAccommodationType("Домик", capacity: 3);
        mock.ReInitProjectInfo();
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Жилец"), mock.Player);
        _ = mock.CreateAccommodationRequest(type, claim);

        _ = Should.Throw<ArgumentNullException>(
            () => new ClaimAccommodationViewModel(ClaimInfo(claim), mock.ProjectInfo, plan: null, neighbours: []));
    }
}
