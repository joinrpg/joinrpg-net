using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Планы всех категорий проекта — <c>IRoomCategoryPlanRepository.GetAllPlans</c> (ADR018).
/// </summary>
/// <remarks>
/// Нужна БД: метод отличается от существующего «план по типу» только снятым фильтром, и проверять
/// надо именно то, что запрос без фильтра переводится в SQL и привозит те же комнаты и группы.
/// Сид смоук-проекта уже содержит тип проживания с парой комнат, расселённых жильцов и одного
/// нерасселённого — ровно те случаи, которые нужны печати конвертов.
/// </remarks>
[Collection(SmokeCollection.Name)]
public class RoomCategoryPlansScenario(SmokeProjectFixture fixture)
{
    [Fact]
    public async Task GetAllPlans_ReturnsPlanWithSeededRoomsAndGroups()
    {
        var plan = await GetSeededPlanAsync();

        plan.Rooms.Select(room => room.Name).ShouldBe(fixture.RoomNames, ignoreOrder: true);
        plan.RoomCapacity.ShouldBe(fixture.RoomCapacity);

        // Группы приезжают все, включая нерасселённую: сид оставляет ровно одну такую.
        plan.Groups.Count.ShouldBe(fixture.Residents.Count);
        plan.UnassignedGroups.Count().ShouldBe(fixture.Residents.Count(r => !r.IsPlaced));
    }

    [Fact]
    public async Task FindRoomByClaim_FindsPlacedClaimsAndNotUnplacedOnes()
    {
        var plan = await GetSeededPlanAsync();

        var placedClaims = plan.Rooms.SelectMany(room => room.Inhabitants).SelectMany(group => group.Subjects).ToList();
        var unplacedClaims = plan.UnassignedGroups.SelectMany(group => group.Subjects).ToList();

        placedClaims.Count.ShouldBe(fixture.Residents.Count(r => r.IsPlaced));
        unplacedClaims.ShouldNotBeEmpty();

        // Комната каждой расселённой заявки — та, в чьих жильцах она и числится.
        foreach (var claimId in placedClaims)
        {
            var room = plan.FindRoomByClaim(claimId).ShouldNotBeNull();
            room.Inhabitants.SelectMany(group => group.Subjects).ShouldContain(claimId);
        }

        foreach (var claimId in unplacedClaims)
        {
            plan.FindRoomByClaim(claimId).ShouldBeNull();
        }
    }

    private async Task<IReadOnlyCollection<RoomCategoryPlan>> GetAllPlansAsync()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IRoomCategoryPlanRepository>()
            .GetAllPlans(fixture.ProjectId);
    }

    private async Task<RoomCategoryPlan> GetSeededPlanAsync() => SeededPlan(await GetAllPlansAsync());

    /// <summary>План категории, из которой селится тип проживания сида.</summary>
    private RoomCategoryPlan SeededPlan(IReadOnlyCollection<RoomCategoryPlan> plans)
        => plans.ShouldHaveSingleItem(
            $"В сид-проекте один тип проживания (id {fixture.RoomTypeId}), значит и план ровно один");
}
