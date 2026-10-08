using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.WebPortal.Managers.Accommodation;

namespace JoinRpg.WebPortal.Managers.Test.Accommodation;

/// <summary>
/// Список целей приглашения к совместному проживанию: группа приглашающего, свободное место и
/// кандидаты берутся из плана поселения, одиночки без типа — отдельным запросом (ADR022).
/// </summary>
public class AccommodationInviteViewServiceTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private ProjectAccommodationType Lux { get; }

    private Claim Sender { get; }

    private ClaimIdentification SenderId => Sender.GetId();

    public AccommodationInviteViewServiceTest()
    {
        Lux = Mock.CreateAccommodationType("Люкс", capacity: 3);
        Mock.ReInitProjectInfo();
        Sender = Mock.CreateApprovedClaim(Mock.CreateCharacter("Приглашающий"), Mock.Player);
    }

    private AccommodationInviteViewService CreateService()
    {
        // Заголовки заявок фейк не выводит из мока сам — кладём все, как их отдал бы репозиторий.
        var claims = new FakeClaimsRepository(Mock);
        claims.Headers.AddRange(Mock.Project.Claims.Select(FakeClaimsRepository.ToHeader));
        return new(
            claims,
            new FakeClaimInfoRepository(Mock),
            new FakeRoomCategoryPlanRepository(Mock),
            // Список целей приглашения не читает и не создаёт — если обратится, тест упадёт.
            accommodationInviteRepository: null!,
            accommodationInviteService: null!,
            new FakeCurrentUserAccessor(Mock.Master.UserId));
    }

    private Claim Approved(string characterName) => Mock.CreateApprovedClaim(Mock.CreateCharacter(characterName), Mock.Player);

    private AccommodationRequest Group(ProjectAccommodationType type, params Claim[] claims)
        => Mock.CreateAccommodationRequest(type, claims);

    private static AccommodationGroupIdentification Target(AccommodationRequest request)
        => AccommodationGroupIdentification.From(
            new AccommodationRequestIdentification(new ProjectIdentification(request.ProjectId), request.Id));

    [Fact]
    public async Task WithoutAccommodationType_NoSenderAndNoTargets()
    {
        _ = Approved("Одиночка");

        var result = await CreateService().GetInviteTargets(SenderId);

        result.SenderRequestId.ShouldBeNull();
        result.Targets.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnplacedGroup_FreeSpaceIsTypeCapacityMinusGroup()
    {
        var own = Group(Lux, Sender);

        var result = await CreateService().GetInviteTargets(SenderId);

        result.SenderRequestId.ShouldBe(new AccommodationRequestIdentification(Mock.ProjectInfo.ProjectId, own.Id));
        result.RoomFreeSpace.ShouldBe(2);
    }

    [Fact]
    public async Task PlacedGroup_FreeSpaceIsByRoom()
    {
        var own = Group(Lux, Sender);
        var room = Mock.CreateRoom(own);
        var roommates = Group(Lux, Approved("Сосед"));
        room.Inhabitants.Add(roommates);
        roommates.Accommodation = room;
        roommates.AccommodationId = room.Id;

        var result = await CreateService().GetInviteTargets(SenderId);

        result.RoomFreeSpace.ShouldBe(1);
    }

    [Fact]
    public async Task Targets_AreUnplacedGroupsOfSameTypeAndSinglesWithoutType()
    {
        var pair = Mock.CreateAccommodationType("Палатка", capacity: 3);
        Mock.ReInitProjectInfo();
        _ = Group(Lux, Sender);

        var sameType = Group(Lux, Approved("Свободный"));
        var placed = Group(Lux, Approved("Расселённый"));
        _ = Mock.CreateRoom(placed);
        _ = Group(pair, Approved("Другой тип"));
        var single = Approved("Без типа");

        var result = await CreateService().GetInviteTargets(SenderId);

        result.Targets.Select(target => target.TargetId).ShouldBe(
            [Target(sameType), AccommodationGroupIdentification.From(single.GetId())],
            ignoreOrder: true);
    }

    [Fact]
    public async Task GroupSize_CountsWholeGroup_NotOnlyApproved()
    {
        // Приём приглашения переселяет весь состав, поэтому и список меряет группу целиком: двоим
        // не хватит одного свободного места, даже если утверждён из них только один.
        _ = Group(Lux, Sender, Approved("Уже сосед"));
        var discussed = Mock.CreateClaim(Mock.CreateCharacter("В обсуждении"), Mock.Player);
        discussed.ClaimStatus = ClaimStatus.Discussed;
        _ = Group(Lux, Approved("Утверждённый"), discussed);

        var result = await CreateService().GetInviteTargets(SenderId);

        result.RoomFreeSpace.ShouldBe(1);
        result.Targets.ShouldBeEmpty();
    }

    [Fact]
    public async Task SingleNotApproved_IsNotTarget()
    {
        _ = Group(Lux, Sender);
        var discussed = Mock.CreateClaim(Mock.CreateCharacter("В обсуждении"), Mock.Player);
        discussed.ClaimStatus = ClaimStatus.Discussed;

        var result = await CreateService().GetInviteTargets(SenderId);

        result.Targets.ShouldBeEmpty();
    }

    [Fact]
    public async Task GroupTarget_IsNamedByAllMembers()
    {
        // Места хватает на всю группу из троих — иначе она не попала бы в список вовсе.
        var big = Mock.CreateAccommodationType("Шатёр", capacity: 4);
        Mock.ReInitProjectInfo();
        _ = Group(big, Sender);
        _ = Group(big, Approved("Арагорн"), Approved("Гимли"), Approved("Леголас"));

        var result = await CreateService().GetInviteTargets(SenderId);

        var target = result.Targets.ShouldHaveSingleItem();
        target.ExtraSearch.ShouldBe("Арагорн, Гимли, Леголас");
        target.Subtext.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task CreateInvite_WithoutAccommodationType_IsRefused()
    {
        var target = AccommodationGroupIdentification.From(Approved("Одиночка").GetId());

        _ = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().CreateInvite(SenderId, target));
    }
}
