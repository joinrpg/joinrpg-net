using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;

namespace JoinRpg.DomainTypes.Test.Accommodation;

/// <summary>
/// Инварианты и расчёты <see cref="RoomCategoryPlan"/> (ADR018).
/// </summary>
public class RoomCategoryPlanTest
{
    private static readonly ProjectIdentification ProjectId = ProjectInfoFixture.ProjectId;
    private static readonly ProjectIdentification OtherProjectId = new(2);

    private const int CategoryIntId = 10;
    private static readonly RoomCategoryIdentification CategoryId = new(ProjectId, CategoryIntId);

    /// <summary>Тип «Люкс»: двухместный, селится из категории <see cref="CategoryId"/></summary>
    private static AccommodationTypeInfo MakeLux(int capacity = 2)
        => ProjectInfoFixture.MakeAccommodationType(
            CategoryIntId, capacity, roomCategoryId: CategoryIntId, name: "Люкс");

    /// <summary>Тип «Люкс на одного» — живёт в том же пуле, но продаётся на одного</summary>
    private static AccommodationTypeInfo MakeLuxSingle(int capacity = 1)
        => ProjectInfoFixture.MakeAccommodationType(
            11, capacity, roomCategoryId: CategoryIntId, name: "Люкс на одного");

    private static ProjectInfo MakeProject(params AccommodationTypeInfo[] types)
        => ProjectInfoFixture.Build(
            accommodationSettings: new ProjectAccommodationSettings(true, types));

    private static AccommodationRoomIdentification RoomId(int id, ProjectIdentification? projectId = null)
        => new(projectId ?? ProjectId, id);

    private static RoomInfo MakeRoom(int id, params AccommodationGroupInfo[] inhabitants)
        => new(RoomId(id), $"Комната {id}", inhabitants);

    private static AccommodationGroupInfo MakeGroup(
        int id,
        AccommodationTypeInfo type,
        int? roomId = null,
        int persons = 1,
        ProjectIdentification? projectId = null)
        => new(
            new AccommodationRequestIdentification(projectId ?? ProjectId, id),
            type.Id,
            roomId is null ? null : RoomId(roomId.Value),
            [.. Enumerable.Range(1, persons).Select(i => new ClaimIdentification(projectId ?? ProjectId, id * 100 + i))]);

    private static RoomCategoryPlan MakePlan(
        ProjectInfo projectInfo,
        IReadOnlyCollection<AccommodationTypeInfo> types,
        int roomCapacity,
        IReadOnlyCollection<RoomInfo> rooms,
        IReadOnlyCollection<AccommodationGroupInfo> groups,
        RoomCategoryIdentification? id = null)
        => new(id ?? CategoryId, projectInfo, types, roomCapacity, rooms, groups);

    #region Инварианты конструктора

    [Fact]
    public void Ctor_ThrowsWhenCategoryBelongsToAnotherProject()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], 2, [], [], id: new RoomCategoryIdentification(OtherProjectId, CategoryIntId)));
    }

    /// <summary>
    /// Инвариант ссылочного равенства ADR013: тип в плане обязан быть тем же экземпляром, что
    /// лежит в метаданных проекта. Здесь тип «такой же», но собран отдельно.
    /// </summary>
    [Fact]
    public void Ctor_ThrowsWhenTypeIsNotInstanceFromProjectInfo()
    {
        var project = MakeProject(MakeLux());
        var copy = MakeLux();

        _ = Should.Throw<ArgumentException>(() => MakePlan(project, [copy], 2, [], []));
    }

    [Fact]
    public void Ctor_ThrowsWhenTypeIsUnknownToProjectInfo()
    {
        var lux = MakeLux();
        var alien = MakeLuxSingle();
        var project = MakeProject(lux);

        _ = Should.Throw<ArgumentException>(() => MakePlan(project, [alien], 2, [], []));
    }

    [Fact]
    public void Ctor_ThrowsWhenRoomBelongsToAnotherProject()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var alienRoom = new RoomInfo(RoomId(1, OtherProjectId), "Чужая", []);

        _ = Should.Throw<ArgumentException>(() => MakePlan(project, [lux], 2, [alienRoom], []));
    }

    [Fact]
    public void Ctor_ThrowsWhenGroupBelongsToAnotherProject()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var alienGroup = MakeGroup(1, lux, projectId: OtherProjectId);

        _ = Should.Throw<ArgumentException>(() => MakePlan(project, [lux], 2, [], [alienGroup]));
    }

    [Fact]
    public void Ctor_ThrowsWhenGroupTypeDoesNotOccupyThisPool()
    {
        var lux = MakeLux();
        var single = MakeLuxSingle();
        var project = MakeProject(lux, single);
        // В плане только «Люкс», а группа куплена по «Люкс на одного»
        var group = MakeGroup(1, single);

        _ = Should.Throw<ArgumentException>(() => MakePlan(project, [lux], 2, [], [group]));
    }

    [Fact]
    public void Ctor_ThrowsWhenInhabitantIsNotAmongGroups()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var stranger = MakeGroup(7, lux, roomId: 1);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], 2, [MakeRoom(1, stranger)], []));
    }

    [Fact]
    public void Ctor_ThrowsWhenAssignedGroupIsNotInAnyRoom()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var group = MakeGroup(1, lux, roomId: 1);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], 2, [MakeRoom(1)], [group]));
    }

    [Fact]
    public void Ctor_ThrowsWhenUnassignedGroupLivesInRoom()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var group = MakeGroup(1, lux, roomId: null);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], 2, [MakeRoom(1, group)], [group]));
    }

    [Fact]
    public void Ctor_ThrowsWhenGroupLivesInTwoRooms()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var group = MakeGroup(1, lux, roomId: 1);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], 2, [MakeRoom(1, group), MakeRoom(2, group)], [group]));
    }

    [Fact]
    public void Ctor_ThrowsWhenGroupRoomIdDoesNotMatchItsRoom()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        // Группа считает, что живёт в первой комнате, а лежит во второй
        var group = MakeGroup(1, lux, roomId: 1);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], 2, [MakeRoom(1), MakeRoom(2, group)], [group]));
    }

    /// <summary>
    /// Переполнения комнаты среди инвариантов нет сознательно (ADR018, §8): вместимость типа
    /// могли уменьшить уже после заселения, и загрузка плана не должна падать на таком проекте.
    /// </summary>
    [Fact]
    public void Ctor_AllowsOverfilledRoom()
    {
        var lux = MakeLux(capacity: 2);
        var project = MakeProject(lux);
        var group = MakeGroup(1, lux, roomId: 1, persons: 5);

        var plan = MakePlan(project, [lux], 2, [MakeRoom(1, group)], [group]);

        plan.GetRoom(RoomId(1)).Occupancy.ShouldBe(5);
        plan.IsFull(RoomId(1)).ShouldBeTrue();
        plan.GetFreeSpace(RoomId(1), lux.Id).ShouldBe(0);
    }

    #endregion

    #region Расчёты

    [Fact]
    public void GetFreeSpace_SubtractsOccupancyFromCapacity()
    {
        var lux = MakeLux(capacity: 4);
        var project = MakeProject(lux);
        var group = MakeGroup(1, lux, roomId: 1, persons: 3);

        var plan = MakePlan(project, [lux], 4, [MakeRoom(1, group), MakeRoom(2)], [group]);

        plan.GetEffectiveCapacity(RoomId(1)).ShouldBe(4);
        plan.GetFreeSpace(RoomId(1), lux.Id).ShouldBe(1);
        plan.GetFreeSpace(RoomId(2), lux.Id).ShouldBe(4);
        plan.IsFull(RoomId(1)).ShouldBeFalse();
    }

    /// <summary>
    /// Главное правило ADR018: тип занимает комнату целиком. Группа типа «Люкс на одного»
    /// делает двухместную комнату полной, хотя физически там осталась койка.
    /// </summary>
    [Fact]
    public void SingleOccupancyType_MakesWholeRoomFull()
    {
        var lux = MakeLux(capacity: 2);
        var single = MakeLuxSingle(capacity: 1);
        var project = MakeProject(lux, single);
        var group = MakeGroup(1, single, roomId: 1, persons: 1);

        var plan = MakePlan(project, [lux, single], roomCapacity: 2, [MakeRoom(1, group)], [group]);

        plan.GetEffectiveCapacity(RoomId(1)).ShouldBe(1);
        plan.IsFull(RoomId(1)).ShouldBeTrue();
        plan.GetFreeSpace(RoomId(1), lux.Id).ShouldBe(0);
        plan.GetFreeSpace(RoomId(1), single.Id).ShouldBe(0);
    }

    /// <summary>
    /// План с двумя типами разной вместимости в одном пуле. Из БД такой план сегодня не
    /// собирается — тип и категория ещё не разделены (ADR018, §2), — но из модели собираться
    /// обязан, иначе задел на разделение фиктивен.
    /// </summary>
    [Fact]
    public void TwoTypesInOnePool_WithDifferentCapacity()
    {
        var lux = MakeLux(capacity: 4);
        var single = MakeLuxSingle(capacity: 2);
        var project = MakeProject(lux, single);

        var plan = MakePlan(project, [lux, single], roomCapacity: 4, [MakeRoom(1), MakeRoom(2)], []);

        plan.AccommodationTypes.Count.ShouldBe(2);
        plan.TotalCapacity.ShouldBe(8);

        // Пустая комната: физический предел ужимается только продаваемой вместимостью типа
        plan.GetEffectiveCapacity(RoomId(1)).ShouldBe(4);
        plan.GetFreeSpace(RoomId(1), lux.Id).ShouldBe(4);
        plan.GetFreeSpace(RoomId(1), single.Id).ShouldBe(2);

        // Въехала группа «на двоих» — комната целиком ограничена её типом
        var group = MakeGroup(1, single, roomId: 1, persons: 2);
        var occupied = MakePlan(project, [lux, single], 4, [MakeRoom(1, group), MakeRoom(2)], [group]);

        occupied.GetEffectiveCapacity(RoomId(1)).ShouldBe(2);
        occupied.IsFull(RoomId(1)).ShouldBeTrue();
        occupied.GetFreeSpace(RoomId(1), lux.Id).ShouldBe(0);
        occupied.GetFreeSpace(RoomId(2), lux.Id).ShouldBe(4);
    }

    [Fact]
    public void TotalCapacity_CountsRoomsOfPool_NotTypes()
    {
        var lux = MakeLux(capacity: 2);
        var single = MakeLuxSingle(capacity: 1);
        var project = MakeProject(lux, single);

        var plan = MakePlan(project, [lux, single], roomCapacity: 2, [MakeRoom(1), MakeRoom(2), MakeRoom(3)], []);

        // 3 комнаты по 2 места — независимо от того, сколько типов из них селится
        plan.TotalCapacity.ShouldBe(6);
    }

    [Fact]
    public void UnassignedGroups_AreGroupsWithoutRoom()
    {
        var lux = MakeLux(capacity: 2);
        var project = MakeProject(lux);
        var settled = MakeGroup(1, lux, roomId: 1, persons: 1);
        var waiting = MakeGroup(2, lux, persons: 2);

        var plan = MakePlan(project, [lux], 2, [MakeRoom(1, settled)], [settled, waiting]);

        plan.UnassignedGroups.Select(g => g.Id).ShouldBe([waiting.Id]);
        plan.Groups.Count.ShouldBe(2);
        plan.GetRoom(RoomId(1)).IsOccupied.ShouldBeTrue();
        waiting.Persons.ShouldBe(2);
    }

    [Fact]
    public void EmptyRoom_IsNotOccupiedAndNotFull()
    {
        var lux = MakeLux(capacity: 2);
        var project = MakeProject(lux);

        var plan = MakePlan(project, [lux], 2, [MakeRoom(1)], []);

        plan.GetRoom(RoomId(1)).IsOccupied.ShouldBeFalse();
        plan.IsFull(RoomId(1)).ShouldBeFalse();
    }

    #endregion

    #region Промахи по идентификатору

    [Fact]
    public void GetRoom_ThrowsForUnknownRoom()
    {
        var lux = MakeLux();
        var plan = MakePlan(MakeProject(lux), [lux], 2, [], []);

        _ = Should.Throw<KeyNotFoundException>(() => plan.GetRoom(RoomId(100500)));
    }

    [Fact]
    public void GetGroup_ThrowsForUnknownGroup()
    {
        var lux = MakeLux();
        var plan = MakePlan(MakeProject(lux), [lux], 2, [], []);

        _ = Should.Throw<KeyNotFoundException>(
            () => plan.GetGroup(new AccommodationRequestIdentification(ProjectId, 100500)));
    }

    [Fact]
    public void GetAccommodationType_ThrowsForTypeFromAnotherPool()
    {
        var lux = MakeLux();
        var single = MakeLuxSingle();
        var plan = MakePlan(MakeProject(lux, single), [lux], 2, [], []);

        _ = Should.Throw<KeyNotFoundException>(() => plan.GetAccommodationType(single.Id));
    }

    [Fact]
    public void GetFreeSpace_ThrowsForTypeFromAnotherPool()
    {
        var lux = MakeLux();
        var single = MakeLuxSingle();
        var plan = MakePlan(MakeProject(lux, single), [lux], 2, [MakeRoom(1)], []);

        _ = Should.Throw<KeyNotFoundException>(() => plan.GetFreeSpace(RoomId(1), single.Id));
    }

    #endregion
}
