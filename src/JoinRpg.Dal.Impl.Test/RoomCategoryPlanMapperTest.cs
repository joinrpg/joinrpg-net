using JoinRpg.Dal.Impl.Repositories.Accommodation;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>
/// Юнит-тесты чистого маппинга <see cref="RoomCategoryPlanMapper.Map"/> (ADR018, §9).
/// БД не используется — только in-memory проекции.
/// </summary>
public class RoomCategoryPlanMapperTest
{
    private readonly MockedProject mock = new();

    private ProjectInfo ProjectInfo => mock.ProjectInfo;

    /// <summary>
    /// Заводит тип проживания и обновляет метаданные проекта: план берёт типы из
    /// <c>ProjectInfo</c>, поэтому без обновления их там не окажется.
    /// </summary>
    private ProjectAccommodationType CreateType(string name = "Палатка", int capacity = 4)
    {
        mock.Project.Details.EnableAccommodation = true;
        var type = mock.CreateAccommodationType(name, capacity: capacity);
        mock.ReInitProjectInfo();
        return type;
    }

    private static RoomCategoryPlanRow MakeRow(
        ProjectAccommodationType category,
        IEnumerable<ProjectAccommodation> rooms,
        IEnumerable<AccommodationRequest> groups)
        => new()
        {
            RoomCategoryId = category.Id,
            RoomCapacity = category.Capacity,
            Rooms = [.. rooms.Select(room => new RoomCategoryPlanRoomRow
            {
                RoomId = room.Id,
                Name = room.Name,
            })],
            Groups = [.. groups.Select(request => new RoomCategoryPlanGroupRow
            {
                GroupId = request.Id,
                AccommodationTypeId = request.AccommodationTypeId,
                RoomId = request.AccommodationId,
                SubjectClaimIds = [.. request.Subjects.Select(claim => claim.ClaimId)],
            })],
        };

    private Claim CreateClaim(string characterName)
        => mock.CreateClaim(mock.CreateCharacter(characterName), mock.Player);

    [Fact]
    public void EmptyCategory_MapsToEmptyPlan()
    {
        var type = CreateType(capacity: 3);

        var plan = RoomCategoryPlanMapper.Map(MakeRow(type, [], []), ProjectInfo);

        plan.Id.ShouldBe(new RoomCategoryIdentification(ProjectInfo.ProjectId, type.Id));
        plan.RoomCapacity.ShouldBe(3);
        plan.Rooms.ShouldBeEmpty();
        plan.Groups.ShouldBeEmpty();
        plan.TotalCapacity.ShouldBe(0);
    }

    [Fact]
    public void RoomsAndGroups_AreMapped()
    {
        var type = CreateType(capacity: 4);
        var claim1 = CreateClaim("Первый");
        var claim2 = CreateClaim("Второй");
        var settled = mock.CreateAccommodationRequest(type, claim1, claim2);
        var room = mock.CreateRoom(settled, "101");
        var waiting = mock.CreateAccommodationRequest(type, CreateClaim("Третий"));

        var plan = RoomCategoryPlanMapper.Map(MakeRow(type, [room], [settled, waiting]), ProjectInfo);

        plan.Rooms.Count.ShouldBe(1);
        plan.TotalCapacity.ShouldBe(4);

        var roomId = new AccommodationRoomIdentification(ProjectInfo.ProjectId, room.Id);
        var roomInfo = plan.GetRoom(roomId);
        roomInfo.Name.ShouldBe("101");
        roomInfo.IsOccupied.ShouldBeTrue();
        roomInfo.Occupancy.ShouldBe(2);

        var settledGroup = plan.GetGroup(new AccommodationRequestIdentification(ProjectInfo.ProjectId, settled.Id));
        settledGroup.RoomId.ShouldBe(roomId);
        settledGroup.SubjectsCount.ShouldBe(2);
        settledGroup.Subjects.ShouldBe(
            [
                new ClaimIdentification(ProjectInfo.ProjectId, claim1.ClaimId),
                new ClaimIdentification(ProjectInfo.ProjectId, claim2.ClaimId),
            ],
            ignoreOrder: true);
        settledGroup.AccommodationTypeId.ShouldBe(
            new AccommodationTypeIdentification(ProjectInfo.ProjectId, type.Id));

        // Нерасселённая группа есть в плане, но ни в одной комнате
        plan.UnassignedGroups.Select(g => g.Id.AccommodationRequestId).ShouldBe([waiting.Id]);
        roomInfo.Inhabitants.Select(i => i.Id).ShouldBe([settledGroup.Id]);
    }

    /// <summary>
    /// Инвариант ссылочного равенства ADR013/ADR018: типы проживания в плане — те же экземпляры,
    /// что в метаданных проекта, а не их копии.
    /// </summary>
    [Fact]
    public void AccommodationTypes_AreTheVeryInstancesFromProjectInfo()
    {
        var type = CreateType();

        var plan = RoomCategoryPlanMapper.Map(MakeRow(type, [], []), ProjectInfo);

        var typeId = new AccommodationTypeIdentification(ProjectInfo.ProjectId, type.Id);
        plan.AccommodationTypes.Count.ShouldBe(1);
        ReferenceEquals(
            plan.GetAccommodationType(typeId),
            ProjectInfo.AccommodationSettings.GetTypeById(typeId))
            .ShouldBeTrue();
    }

    /// <summary>
    /// Пул собирается по категории, а не по типу: комнаты и группы чужой категории в план
    /// не попадают, и её тип не числится среди селящихся из этого пула.
    /// </summary>
    [Fact]
    public void ForeignCategory_IsNotInPlan()
    {
        var tent = CreateType("Палатка", capacity: 4);
        var lux = CreateType("Люкс", capacity: 2);

        var plan = RoomCategoryPlanMapper.Map(MakeRow(tent, [], []), ProjectInfo);

        plan.AccommodationTypes.Single().Name.ShouldBe("Палатка");
        _ = Should.Throw<AccommodationTypeNotFoundException>(
            () => plan.GetAccommodationType(new AccommodationTypeIdentification(ProjectInfo.ProjectId, lux.Id)));
    }

    /// <summary>
    /// Тест-страж миграции ADR018 (§14, PR 2): доменный <c>GetFreeSpace</c> считает свободное место
    /// ровно так же, как legacy <see cref="AccommodationExtensions.GetRoomFreeSpace(ProjectAccommodation)"/>
    /// поверх EF-сущностей — на одном и том же наборе данных. Пока тип и категория не разделены,
    /// формула ADR018 вырождается в нынешнюю, и это должно быть видно тестом, а не на глаз.
    /// Расхождение появится только вместе с разделением — тогда тест и надо пересмотреть.
    /// </summary>
    [Theory]
    [InlineData(4, 0)]
    [InlineData(4, 1)]
    [InlineData(4, 4)]
    // Вместимость уменьшили уже после заселения — комната переполнена
    [InlineData(1, 2)]
    public void GetFreeSpace_MatchesLegacyGetRoomFreeSpace(int capacity, int inhabitants)
    {
        var type = CreateType(capacity: capacity);
        var claims = Enumerable.Range(1, inhabitants)
            .Select(i => CreateClaim("Жилец " + i))
            .ToArray();
        var group = mock.CreateAccommodationRequest(type, claims);
        var room = mock.CreateRoom(group, "101");
        if (inhabitants == 0)
        {
            // Комната без жильцов: группа заведена, но в комнату не расселена
            room.Inhabitants = [];
            group.Accommodation = null;
            group.AccommodationId = null;
        }

        var plan = RoomCategoryPlanMapper.Map(MakeRow(type, [room], [group]), ProjectInfo);

        var freeSpace = plan.GetFreeSpace(
            new AccommodationRoomIdentification(ProjectInfo.ProjectId, room.Id),
            new AccommodationTypeIdentification(ProjectInfo.ProjectId, type.Id));

        // Legacy умеет уходить в минус, доменный метод — нет (ADR018, §14, PR 1)
        freeSpace.ShouldBe(Math.Max(0, room.GetRoomFreeSpace()));
    }
}
