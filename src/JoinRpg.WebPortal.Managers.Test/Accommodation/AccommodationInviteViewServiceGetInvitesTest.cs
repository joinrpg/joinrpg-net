using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation;
using JoinRpg.WebPortal.Managers.Accommodation;

namespace JoinRpg.WebPortal.Managers.Test.Accommodation;

/// <summary>
/// Панель приглашений к совместному проживанию: принятые и приглашения от/к своей группе скрыты,
/// группа сравнивается по внешнему ключу заявки, без навигации на неё (ADR022).
/// </summary>
public class AccommodationInviteViewServiceGetInvitesTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private ProjectAccommodationType Lux { get; }

    private Claim Own { get; }

    private ClaimIdentification OwnId => Own.GetId();

    private int nextUserId = 200;

    public AccommodationInviteViewServiceGetInvitesTest()
    {
        Lux = Mock.CreateAccommodationType("Люкс", capacity: 4);
        Mock.ReInitProjectInfo();
        Own = Mock.CreateApprovedClaim(Mock.CreateCharacter("Своя заявка"), Mock.Player);
    }

    private AccommodationInviteViewService CreateService()
        => new(
            new FakeClaimsRepository(Mock),
            claimInfoRepository: null!,
            roomCategoryPlanRepository: null!,
            new FakeAccommodationInviteRepository(Mock),
            accommodationInviteService: null!,
            new FakeCurrentUserAccessor(Mock.Master.UserId));

    /// <summary>Утверждённая заявка отдельного игрока — чтобы контрагента было видно по пользователю.</summary>
    private Claim Other(string name)
    {
        var user = new User
        {
            UserId = nextUserId++,
            PrefferedName = name,
            Email = $"user{nextUserId}@example.com",
            Claims = new HashSet<Claim>(),
        };
        return Mock.CreateApprovedClaim(Mock.CreateCharacter(name), user);
    }

    private static UserIdentification UserOf(Claim claim) => new(claim.PlayerUserId);

    private Task<IReadOnlyCollection<AccommodationInviteViewModel>> Incoming()
        => CreateService().GetInvites(OwnId, InviteDirection.Incoming);

    private Task<IReadOnlyCollection<AccommodationInviteViewModel>> Outgoing()
        => CreateService().GetInvites(OwnId, InviteDirection.Outgoing);

    [Fact]
    public async Task Incoming_Unanswered_IsVisibleWithSender()
    {
        _ = Mock.CreateAccommodationRequest(Lux, Own);
        var from = Other("Пригласивший");
        _ = Mock.CreateAccommodationRequest(Lux, from);
        var invite = Mock.CreateAccommodationInvite(from, Own);

        var result = await Incoming();

        var item = result.ShouldHaveSingleItem();
        item.InviteId.ShouldBe(new AccommodationInviteIdentification(Mock.ProjectInfo.ProjectId, invite.Id));
        item.Counterparty.UserId.ShouldBe(UserOf(from));
        item.State.ShouldBe(InviteState.Unanswered);
    }

    [Fact]
    public async Task Incoming_Accepted_IsHidden()
    {
        _ = Mock.CreateAccommodationRequest(Lux, Own);
        var from = Other("Пригласивший");
        _ = Mock.CreateAccommodationRequest(Lux, from);
        var invite = Mock.CreateAccommodationInvite(from, Own);
        invite.IsAccepted = InviteState.Accepted;

        var result = await Incoming();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Incoming_Declined_IsVisible()
    {
        _ = Mock.CreateAccommodationRequest(Lux, Own);
        var from = Other("Пригласивший");
        _ = Mock.CreateAccommodationRequest(Lux, from);
        var invite = Mock.CreateAccommodationInvite(from, Own);
        invite.IsAccepted = InviteState.Declined;

        var result = await Incoming();

        result.ShouldHaveSingleItem().State.ShouldBe(InviteState.Declined);
    }

    [Fact]
    public async Task Incoming_FromMemberOfOwnGroup_IsHidden()
    {
        var roommate = Other("Сосед");
        _ = Mock.CreateAccommodationRequest(Lux, Own, roommate);
        _ = Mock.CreateAccommodationInvite(roommate, Own);

        var result = await Incoming();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Incoming_FromClaimWithoutGroup_WhileOwnInGroup_IsVisible()
    {
        _ = Mock.CreateAccommodationRequest(Lux, Own);
        var single = Other("Без типа");
        _ = Mock.CreateAccommodationInvite(single, Own);

        var result = await Incoming();

        result.ShouldHaveSingleItem().Counterparty.UserId.ShouldBe(UserOf(single));
    }

    [Fact]
    public async Task Incoming_OnlySameGroupIsHidden_OthersStay()
    {
        var roommate = Other("Сосед");
        _ = Mock.CreateAccommodationRequest(Lux, Own, roommate);
        var stranger = Other("Чужой");
        _ = Mock.CreateAccommodationRequest(Lux, stranger);
        _ = Mock.CreateAccommodationInvite(roommate, Own);
        _ = Mock.CreateAccommodationInvite(stranger, Own);

        var result = await Incoming();

        result.ShouldHaveSingleItem().Counterparty.UserId.ShouldBe(UserOf(stranger));
    }

    [Fact]
    public async Task Outgoing_IsVisibleWithRecipient()
    {
        _ = Mock.CreateAccommodationRequest(Lux, Own);
        var to = Other("Приглашённый");
        _ = Mock.CreateAccommodationRequest(Lux, to);
        var invite = Mock.CreateAccommodationInvite(Own, to);
        // Встречное приглашение — входящее, в исходящих его быть не должно.
        _ = Mock.CreateAccommodationInvite(Other("Встречный"), Own);

        var result = await Outgoing();

        var item = result.ShouldHaveSingleItem();
        item.InviteId.ShouldBe(new AccommodationInviteIdentification(Mock.ProjectInfo.ProjectId, invite.Id));
        item.Counterparty.UserId.ShouldBe(UserOf(to));
    }

    [Fact]
    public async Task Outgoing_ToMemberOfOwnGroup_IsHidden()
    {
        var roommate = Other("Сосед");
        _ = Mock.CreateAccommodationRequest(Lux, Own, roommate);
        _ = Mock.CreateAccommodationInvite(Own, roommate);

        var result = await Outgoing();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task BothWithoutGroup_AreNotConsideredSameGroup()
    {
        // У обеих сторон FK группы пуст: null == null не должно значить «одна группа».
        var single = Other("Тоже без типа");
        _ = Mock.CreateAccommodationInvite(single, Own);
        _ = Mock.CreateAccommodationInvite(Own, single);

        var incoming = await Incoming();
        var outgoing = await Outgoing();

        incoming.ShouldHaveSingleItem().Counterparty.UserId.ShouldBe(UserOf(single));
        outgoing.ShouldHaveSingleItem().Counterparty.UserId.ShouldBe(UserOf(single));
    }

    [Fact]
    public async Task OwnWithoutGroup_InviteFromGroupedClaim_IsVisible()
    {
        var from = Other("В группе");
        _ = Mock.CreateAccommodationRequest(Lux, from);
        _ = Mock.CreateAccommodationInvite(from, Own);

        var result = await Incoming();

        result.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task NoInvites_Empty()
    {
        var result = await Incoming();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task PlayerOfOtherClaim_HasNoAccess()
    {
        var stranger = Other("Посторонний");
        var service = new AccommodationInviteViewService(
            new FakeClaimsRepository(Mock),
            claimInfoRepository: null!,
            roomCategoryPlanRepository: null!,
            new FakeAccommodationInviteRepository(Mock),
            accommodationInviteService: null!,
            new FakeCurrentUserAccessor(stranger.PlayerUserId));

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => service.GetInvites(OwnId, InviteDirection.Incoming));
    }
}
