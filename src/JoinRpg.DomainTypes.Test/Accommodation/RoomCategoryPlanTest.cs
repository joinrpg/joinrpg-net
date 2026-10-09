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

    /// <summary>Вторая категория комнат — для тестов, которым нужно два пула.</summary>
    private const int SecondCategoryIntId = 11;

    /// <summary>Тип «Люкс»: двухместный, селится из категории <see cref="CategoryId"/></summary>
    private static AccommodationTypeInfo MakeLux(int capacity = 2)
        => ProjectInfoFixture.MakeAccommodationType(
            CategoryIntId, capacity, roomCategoryId: CategoryIntId, name: "Люкс");

    /// <summary>
    /// Тип «Люкс на одного» — по умолчанию живёт в том же пуле, но продаётся на одного.
    /// <paramref name="roomCategoryId"/> нужен тестам про второй пул: тип, селящийся из другой
    /// категории, обязан и ссылаться на неё, иначе конфигурация невозможная.
    /// </summary>
    private static AccommodationTypeInfo MakeLuxSingle(int capacity = 1, int? roomCategoryId = null)
        => ProjectInfoFixture.MakeAccommodationType(
            11, capacity, roomCategoryId: roomCategoryId ?? CategoryIntId, name: "Люкс на одного");

    private static ProjectInfo MakeProject(params AccommodationTypeInfo[] types)
        => ProjectInfoFixture.Build(
            accommodationSettings: ProjectInfoFixture.MakeAccommodationSettings(types));

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
        IReadOnlyCollection<RoomInfo> rooms,
        IReadOnlyCollection<AccommodationGroupInfo> groups,
        RoomCategoryIdentification? id = null)
        => new(id ?? CategoryId, projectInfo, types, rooms, groups);

    #region Инварианты конструктора

    [Fact]
    public void Ctor_ThrowsWhenCategoryBelongsToAnotherProject()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], [], [], id: new RoomCategoryIdentification(OtherProjectId, CategoryIntId)));
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

        _ = Should.Throw<ArgumentException>(() => MakePlan(project, [copy], [], []));
    }

    [Fact]
    public void Ctor_ThrowsWhenTypeIsUnknownToProjectInfo()
    {
        var lux = MakeLux();
        var alien = MakeLuxSingle();
        var project = MakeProject(lux);

        _ = Should.Throw<ArgumentException>(() => MakePlan(project, [alien], [], []));
    }

    /// <summary>Тип, селящийся из другой категории, в план этой категории не входит (ADR020).</summary>
    [Fact]
    public void Ctor_ThrowsWhenTypeBelongsToAnotherCategory()
    {
        var lux = MakeLux();
        var otherPool = MakeLuxSingle(roomCategoryId: SecondCategoryIntId);
        var project = MakeProject(lux, otherPool);

        _ = Should.Throw<ArgumentException>(() => MakePlan(project, [lux, otherPool], [], []));
    }

    /// <summary>Пул без типов вмещает ноль, а не падает на пустом максимуме.</summary>
    [Fact]
    public void RoomCapacity_OfPoolWithoutTypes_IsZero()
    {
        var plan = MakePlan(MakeProject(), [], [MakeRoom(1)], []);

        plan.RoomCapacity.ShouldBe(0);
        plan.TotalCapacity.ShouldBe(0);
    }

    [Fact]
    public void Ctor_ThrowsWhenRoomBelongsToAnotherProject()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var alienRoom = new RoomInfo(RoomId(1, OtherProjectId), "Чужая", []);

        _ = Should.Throw<ArgumentException>(() => MakePlan(project, [lux], [alienRoom], []));
    }

    [Fact]
    public void Ctor_ThrowsWhenGroupBelongsToAnotherProject()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var alienGroup = MakeGroup(1, lux, projectId: OtherProjectId);

        _ = Should.Throw<ArgumentException>(() => MakePlan(project, [lux], [], [alienGroup]));
    }

    [Fact]
    public void Ctor_ThrowsWhenGroupTypeDoesNotOccupyThisPool()
    {
        var lux = MakeLux();
        var single = MakeLuxSingle();
        var project = MakeProject(lux, single);
        // В плане только «Люкс», а группа куплена по «Люкс на одного»
        var group = MakeGroup(1, single);

        _ = Should.Throw<ArgumentException>(() => MakePlan(project, [lux], [], [group]));
    }

    [Fact]
    public void Ctor_ThrowsWhenInhabitantIsNotAmongGroups()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var stranger = MakeGroup(7, lux, roomId: 1);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], [MakeRoom(1, stranger)], []));
    }

    [Fact]
    public void Ctor_ThrowsWhenAssignedGroupIsNotInAnyRoom()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var group = MakeGroup(1, lux, roomId: 1);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], [MakeRoom(1)], [group]));
    }

    [Fact]
    public void Ctor_ThrowsWhenUnassignedGroupLivesInRoom()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var group = MakeGroup(1, lux, roomId: null);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], [MakeRoom(1, group)], [group]));
    }

    [Fact]
    public void Ctor_ThrowsWhenGroupLivesInTwoRooms()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        var group = MakeGroup(1, lux, roomId: 1);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], [MakeRoom(1, group), MakeRoom(2, group)], [group]));
    }

    [Fact]
    public void Ctor_ThrowsWhenGroupRoomIdDoesNotMatchItsRoom()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);
        // Группа считает, что живёт в первой комнате, а лежит во второй
        var group = MakeGroup(1, lux, roomId: 1);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], [MakeRoom(1), MakeRoom(2, group)], [group]));
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

        var plan = MakePlan(project, [lux], [MakeRoom(1, group)], [group]);

        plan.GetRoom(RoomId(1)).Occupancy.ShouldBe(5);
        plan.IsFull(RoomId(1)).ShouldBeTrue();
        plan.GetFreeSpace(RoomId(1), lux.Id).ShouldBe(0);
    }

    [Fact]
    public void Constructor_ThrowsWhenClaimIsSubjectOfTwoGroups()
    {
        var lux = MakeLux();
        var project = MakeProject(lux);

        var first = MakeGroup(1, lux, roomId: 1);
        var sharedClaim = first.Subjects.Single();
        // Вторая группа в другой комнате претендует на ту же заявку.
        var second = new AccommodationGroupInfo(
            new AccommodationRequestIdentification(ProjectId, 2), lux.Id, RoomId(2), [sharedClaim]);

        _ = Should.Throw<ArgumentException>(
            () => MakePlan(project, [lux], [MakeRoom(1, first), MakeRoom(2, second)], [first, second]));
    }

    #endregion

    #region Расчёты

    [Fact]
    public void GetFreeSpace_SubtractsOccupancyFromCapacity()
    {
        var lux = MakeLux(capacity: 4);
        var project = MakeProject(lux);
        var group = MakeGroup(1, lux, roomId: 1, persons: 3);

        var plan = MakePlan(project, [lux], [MakeRoom(1, group), MakeRoom(2)], [group]);

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

        var plan = MakePlan(project, [lux, single], [MakeRoom(1, group)], [group]);

        plan.GetEffectiveCapacity(RoomId(1)).ShouldBe(1);
        plan.IsFull(RoomId(1)).ShouldBeTrue();
        plan.GetFreeSpace(RoomId(1), lux.Id).ShouldBe(0);
        plan.GetFreeSpace(RoomId(1), single.Id).ShouldBe(0);
    }

    /// <summary>
    /// План с двумя типами разной вместимости в одном пуле (ADR020). Физическая вместимость пула —
    /// наибольшая из вместимостей его типов.
    /// </summary>
    [Fact]
    public void TwoTypesInOnePool_WithDifferentCapacity()
    {
        var lux = MakeLux(capacity: 4);
        var single = MakeLuxSingle(capacity: 2);
        var project = MakeProject(lux, single);

        var plan = MakePlan(project, [lux, single], [MakeRoom(1), MakeRoom(2)], []);

        plan.AccommodationTypes.Count.ShouldBe(2);
        plan.RoomCapacity.ShouldBe(4);
        plan.TotalCapacity.ShouldBe(8);

        // Пустая комната: физический предел ужимается только продаваемой вместимостью типа
        plan.GetEffectiveCapacity(RoomId(1)).ShouldBe(4);
        plan.GetFreeSpace(RoomId(1), lux.Id).ShouldBe(4);
        plan.GetFreeSpace(RoomId(1), single.Id).ShouldBe(2);

        // Въехала группа «на двоих» — комната целиком ограничена её типом
        var group = MakeGroup(1, single, roomId: 1, persons: 2);
        var occupied = MakePlan(project, [lux, single], [MakeRoom(1, group), MakeRoom(2)], [group]);

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

        var plan = MakePlan(project, [lux, single], [MakeRoom(1), MakeRoom(2), MakeRoom(3)], []);

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

        var plan = MakePlan(project, [lux], [MakeRoom(1, settled)], [settled, waiting]);

        plan.UnassignedGroups.Select(g => g.Id).ShouldBe([waiting.Id]);
        plan.Groups.Count.ShouldBe(2);
        plan.GetRoom(RoomId(1)).IsOccupied.ShouldBeTrue();
        waiting.SubjectsCount.ShouldBe(2);
    }

    [Fact]
    public void EmptyRoom_IsNotOccupiedAndNotFull()
    {
        var lux = MakeLux(capacity: 2);
        var project = MakeProject(lux);

        var plan = MakePlan(project, [lux], [MakeRoom(1)], []);

        plan.GetRoom(RoomId(1)).IsOccupied.ShouldBeFalse();
        plan.IsFull(RoomId(1)).ShouldBeFalse();
    }

    /// <summary>
    /// Свободные места пула — по эффективной вместимости: комната, которую «Люкс на одного»
    /// сделал полной, мест не даёт, хотя физически в ней вторая койка.
    /// </summary>
    [Fact]
    public void FreeCapacity_CountsEffectiveCapacityOfEachRoom()
    {
        var lux = MakeLux(capacity: 2);
        var single = MakeLuxSingle(capacity: 1);
        var project = MakeProject(lux, single);
        var alone = MakeGroup(1, single, roomId: 1, persons: 1);
        var pair = MakeGroup(2, lux, roomId: 2, persons: 1);

        var plan = MakePlan(project, [lux, single], [MakeRoom(1, alone), MakeRoom(2, pair), MakeRoom(3)], [alone, pair]);

        plan.Occupancy.ShouldBe(2);
        plan.TotalCapacity.ShouldBe(6);
        // 0 в комнате «на одного» + 1 рядом с парой + 2 в пустой
        plan.FreeCapacity.ShouldBe(3);
    }

    #endregion

    #region Группа заявки, свободное место для группы, соседи (ADR022)

    [Fact]
    public void GetGroupOrDefault_ReturnsGroupForGroupReference()
    {
        var lux = MakeLux();
        var group = MakeGroup(1, lux);
        var plan = MakePlan(MakeProject(lux), [lux], [], [group]);

        plan.GetGroupOrDefault(AccommodationGroupIdentification.From(group.Id)).ShouldBe(group);
    }

    [Fact]
    public void GetGroupOrDefault_ReturnsNullForSoloClaim()
    {
        var lux = MakeLux();
        var plan = MakePlan(MakeProject(lux), [lux], [], []);

        plan.GetGroupOrDefault(AccommodationGroupIdentification.From(new ClaimIdentification(ProjectId, 5)))
            .ShouldBeNull();
    }

    [Fact]
    public void GetGroupOrDefault_ThrowsForGroupOutsidePlan()
    {
        var lux = MakeLux();
        var plan = MakePlan(MakeProject(lux), [lux], [], []);

        _ = Should.Throw<AccommodationGroupNotFoundException>(
            () => plan.GetGroupOrDefault(
                AccommodationGroupIdentification.From(new AccommodationRequestIdentification(ProjectId, 99))));
    }

    // Сценарии ниже повторяют AccommodationRoomFreeSpaceTest — тесты EF-расчёта, который этот метод
    // заменяет: на непереполненных данных ответы обязаны совпадать.

    [Fact]
    public void GetFreeSpaceForGroup_PlacedGroup_CapacityMinusOccupancy()
    {
        var lux = MakeLux(capacity: 3);
        var group = MakeGroup(1, lux, roomId: 1, persons: 1);
        var plan = MakePlan(MakeProject(lux), [lux], [MakeRoom(1, group)], [group]);

        plan.GetFreeSpaceForGroup(group.Id).ShouldBe(2);
    }

    [Fact]
    public void GetFreeSpaceForGroup_UnassignedGroup_CapacityOfOwnTypeMinusGroupSize()
    {
        var lux = MakeLux(capacity: 3);
        var group = MakeGroup(1, lux, persons: 2);
        var plan = MakePlan(MakeProject(lux), [lux], [], [group]);

        plan.GetFreeSpaceForGroup(group.Id).ShouldBe(1);
    }

    [Fact]
    public void GetFreeSpaceForGroup_RoomWithSiblingTypeGroup_IsLimitedByItsCapacity()
    {
        // «Люкс на двоих», въехавший в трёхместный «Люкс», делает комнату двухместной — и для
        // группы «Люкса» тоже.
        var lux = MakeLux(capacity: 3);
        var pair = MakeLuxSingle(capacity: 2);
        var luxGroup = MakeGroup(1, lux, roomId: 1);
        var pairGroup = MakeGroup(2, pair, roomId: 1);
        var plan = MakePlan(
            MakeProject(lux, pair), [lux, pair], [MakeRoom(1, luxGroup, pairGroup)], [luxGroup, pairGroup]);

        plan.GetFreeSpaceForGroup(luxGroup.Id).ShouldBe(0);
    }

    [Fact]
    public void GetFreeSpaceForGroup_IsNeverNegative()
    {
        // Вместимость типа уменьшили после того, как группа сложилась: EF-расчёт давал здесь −1.
        var lux = MakeLux(capacity: 2);
        var placed = MakeGroup(1, lux, roomId: 1, persons: 3);
        var unassigned = MakeGroup(2, lux, persons: 3);
        var plan = MakePlan(MakeProject(lux), [lux], [MakeRoom(1, placed)], [placed, unassigned]);

        plan.GetFreeSpaceForGroup(placed.Id).ShouldBe(0);
        plan.GetFreeSpaceForGroup(unassigned.Id).ShouldBe(0);
    }

    [Fact]
    public void GetNeighbours_PlacedGroup_AreAllRoomInhabitantsButSelf()
    {
        var lux = MakeLux(capacity: 4);
        var mine = MakeGroup(1, lux, roomId: 1, persons: 2);
        var other = MakeGroup(2, lux, roomId: 1, persons: 1);
        var plan = MakePlan(MakeProject(lux), [lux], [MakeRoom(1, mine, other)], [mine, other]);
        var me = mine.Subjects.First();

        plan.GetNeighbours(me).ShouldBe(
            [.. mine.Subjects.Concat(other.Subjects).Where(id => id != me)], ignoreOrder: true);
    }

    [Fact]
    public void GetNeighbours_UnassignedGroup_AreGroupMembersButSelf()
    {
        var lux = MakeLux(capacity: 4);
        var mine = MakeGroup(1, lux, persons: 3);
        var plan = MakePlan(MakeProject(lux), [lux], [], [mine]);
        var me = mine.Subjects.First();

        plan.GetNeighbours(me).ShouldBe([.. mine.Subjects.Skip(1)], ignoreOrder: true);
    }

    [Fact]
    public void GetFreeSpaceForGroup_EmptyUnassignedGroup_IsTypeCapacity()
    {
        // Группы без жильцов в БД встречаются.
        var lux = MakeLux(capacity: 3);
        var empty = MakeGroup(1, lux, persons: 0);
        var plan = MakePlan(MakeProject(lux), [lux], [], [empty]);

        plan.GetFreeSpaceForGroup(empty.Id).ShouldBe(3);
    }

    [Fact]
    public void GetNeighbours_AloneInRoom_IsEmpty()
    {
        var lux = MakeLux(capacity: 2);
        var mine = MakeGroup(1, lux, roomId: 1);
        var plan = MakePlan(MakeProject(lux), [lux], [MakeRoom(1, mine)], [mine]);

        plan.GetNeighbours(mine.Subjects.Single()).ShouldBeEmpty();
    }

    [Fact]
    public void GetNeighbours_ClaimOutsidePlan_IsEmpty()
    {
        var lux = MakeLux();
        var plan = MakePlan(MakeProject(lux), [lux], [], [MakeGroup(1, lux)]);

        plan.GetNeighbours(new ClaimIdentification(ProjectId, 100500)).ShouldBeEmpty();
    }

    #endregion

    #region Поиск комнаты по заявке

    [Fact]
    public void FindRoomByClaim_ReturnsRoomOfPlacedGroup()
    {
        var lux = MakeLux();
        var placed = MakeGroup(1, lux, roomId: 1, persons: 2);
        var plan = MakePlan(MakeProject(lux), [lux], [MakeRoom(1, placed), MakeRoom(2)], [placed]);

        // Любой из жильцов группы живёт в её комнате — не только первый.
        foreach (var claimId in placed.Subjects)
        {
            plan.FindRoomByClaim(claimId).ShouldBe(plan.GetRoom(RoomId(1)));
        }
    }

    [Fact]
    public void FindRoomByClaim_ReturnsNullForUnassignedGroup()
    {
        var lux = MakeLux();
        var unassigned = MakeGroup(1, lux, roomId: null);
        var plan = MakePlan(MakeProject(lux), [lux], [MakeRoom(1)], [unassigned]);

        plan.FindRoomByClaim(unassigned.Subjects.Single()).ShouldBeNull();
    }

    [Fact]
    public void FindRoomByClaim_ReturnsNullForClaimOutsidePlan()
    {
        var lux = MakeLux();
        var placed = MakeGroup(1, lux, roomId: 1);
        var plan = MakePlan(MakeProject(lux), [lux], [MakeRoom(1, placed)], [placed]);

        plan.FindRoomByClaim(new ClaimIdentification(ProjectId, 100500)).ShouldBeNull();
    }

    [Fact]
    public void FindRoomByClaim_AnswersPerPlan_NotAcrossPlans()
    {
        var lux = MakeLux();
        var single = MakeLuxSingle(roomCategoryId: SecondCategoryIntId);
        var project = MakeProject(lux, single);

        var inLux = MakeGroup(1, lux, roomId: 1, persons: 2);
        var luxPlan = MakePlan(project, [lux], [MakeRoom(1, inLux), MakeRoom(2)], [inLux]);

        var inSingle = MakeGroup(3, single, roomId: 3);
        var singlePlan = MakePlan(
            project,
            [single],
            [MakeRoom(3, inSingle)],
            [inSingle],
            id: new RoomCategoryIdentification(ProjectId, SecondCategoryIntId));

        // Каждый план знает только своих жильцов: вызывающий, у которого планов несколько,
        // спрашивает их по очереди — своего индекса поверх коллекции для этого не нужно.
        luxPlan.FindRoomByClaim(inLux.Subjects.First()).ShouldBe(luxPlan.GetRoom(RoomId(1)));
        luxPlan.FindRoomByClaim(inSingle.Subjects.Single()).ShouldBeNull();
        singlePlan.FindRoomByClaim(inSingle.Subjects.Single()).ShouldBe(singlePlan.GetRoom(RoomId(3)));
        singlePlan.FindRoomByClaim(inLux.Subjects.First()).ShouldBeNull();
    }
    #endregion

    #region Промахи по идентификатору

    [Fact]
    public void GetRoom_ThrowsForUnknownRoom()
    {
        var lux = MakeLux();
        var plan = MakePlan(MakeProject(lux), [lux], [], []);

        _ = Should.Throw<AccommodationRoomNotFoundException>(() => plan.GetRoom(RoomId(100500)));
    }

    [Fact]
    public void GetGroup_ThrowsForUnknownGroup()
    {
        var lux = MakeLux();
        var plan = MakePlan(MakeProject(lux), [lux], [], []);

        _ = Should.Throw<AccommodationGroupNotFoundException>(
            () => plan.GetGroup(new AccommodationRequestIdentification(ProjectId, 100500)));
    }

    [Fact]
    public void GetAccommodationType_ThrowsForTypeFromAnotherPool()
    {
        var lux = MakeLux();
        var single = MakeLuxSingle();
        var plan = MakePlan(MakeProject(lux, single), [lux], [], []);

        _ = Should.Throw<AccommodationTypeNotFoundException>(() => plan.GetAccommodationType(single.Id));
    }

    [Fact]
    public void GetFreeSpace_ThrowsForTypeFromAnotherPool()
    {
        var lux = MakeLux();
        var single = MakeLuxSingle();
        var plan = MakePlan(MakeProject(lux, single), [lux], [MakeRoom(1)], []);

        _ = Should.Throw<AccommodationTypeNotFoundException>(() => plan.GetFreeSpace(RoomId(1), single.Id));
    }

    #endregion
}
