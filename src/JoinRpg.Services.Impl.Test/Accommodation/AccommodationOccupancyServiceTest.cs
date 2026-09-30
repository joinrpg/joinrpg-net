using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Тесты заселения и выселения (<c>OccupyRoom</c>/<c>UnOccupy*</c>) поверх агрегата плана
/// поселения (ADR018, PR 5).
/// </summary>
/// <remarks>
/// Тесты воспроизводят дефекты 3, 5, 6 и 7 ADR018: массовое выселение делало сохранение на каждую
/// комнату, заявку одного типа можно было поселить в комнату другого, свободное место считалось
/// по навигации EF (relationship fixup), а промах по комнате или группе давал NRE.
/// </remarks>
public class AccommodationOccupancyServiceTest : AccommodationServiceTestBase
{
    private readonly ProjectAccommodationType tent;

    public AccommodationOccupancyServiceTest()
    {
        tent = mock.CreateAccommodationType("Палатка", capacity: 4);
        mock.ReInitProjectInfo();
    }

    private AccommodationTypeIdentification TentId => new(ProjectId, tent.Id);

    /// <summary>Группа из указанного числа игроков нужного типа проживания.</summary>
    private AccommodationRequest CreateGroup(ProjectAccommodationType type, int persons = 1)
    {
        var claims = new Claim[persons];
        for (var i = 0; i < persons; i++)
        {
            claims[i] = mock.CreateClaim(mock.CreateCharacter($"Персонаж {mock.AccommodationRequests.Count}-{i}"), mock.Player);
        }
        return mock.CreateAccommodationRequest(type, claims);
    }

    [Fact]
    public async Task OccupyRoom_MovesGroupIntoRoom()
    {
        var group = CreateGroup(tent);
        var room = mock.CreateEmptyRoom(tent, "101");

        await CreateService().OccupyRoom(RoomId(room), [GroupId(group)]);

        group.AccommodationId.ShouldBe(room.Id);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>Письма легаси-канала обязаны уходить уже после сохранения (ADR018, §11).</summary>
    [Fact]
    public async Task OccupyRoom_SendsEmailAfterSave()
    {
        var group = CreateGroup(tent);
        var room = mock.CreateEmptyRoom(tent, "101");

        await CreateService().OccupyRoom(RoomId(room), [GroupId(group)]);

        journal.ShouldBe(["save", "email"]);
        emailService.Sent.ShouldHaveSingleItem().ShouldBeOfType<OccupyRoomEmail>();
    }

    [Fact]
    public async Task OccupyRoom_WithoutPermission_Throws()
    {
        var group = CreateGroup(tent);
        var room = mock.CreateEmptyRoom(tent, "101");

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).OccupyRoom(RoomId(room), [GroupId(group)]));

        group.AccommodationId.ShouldBeNull();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 4 ADR018: активность проекта не проверял ни один метод.</summary>
    [Fact]
    public async Task OccupyRoom_InArchivedProject_Throws()
    {
        var group = CreateGroup(tent);
        var room = mock.CreateEmptyRoom(tent, "101");
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().OccupyRoom(RoomId(room), [GroupId(group)]));

        group.AccommodationId.ShouldBeNull();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Дефект 5 ADR018: комната грузилась по <c>(ProjectId, RoomId)</c>, группа — по
    /// <c>(ProjectId, Id)</c>, и заявку на палатку можно было поселить в гостиничный номер.
    /// Теперь и комната, и группа берутся из одного плана, и группа чужого пула в нём не найдётся.
    /// </summary>
    [Fact]
    public async Task OccupyRoom_GroupFromAnotherPool_Throws()
    {
        var hotel = mock.CreateAccommodationType("Отель", capacity: 4);
        mock.ReInitProjectInfo();

        var tentGroup = CreateGroup(tent);
        var hotelRoom = mock.CreateEmptyRoom(hotel, "101");

        _ = await Should.ThrowAsync<AccommodationGroupNotFoundException>(
            () => CreateService().OccupyRoom(RoomId(hotelRoom), [GroupId(tentGroup)]));

        tentGroup.AccommodationId.ShouldBeNull();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Дефект 6 ADR018: свободное место считалось по <c>room.Inhabitants</c>, то есть корректность
    /// цикла держалась на relationship fixup трекера EF6. Теперь место считается по снимку плана
    /// и пересчитывается по мере заселения — вторая группа в ту же операцию уже не влезает.
    /// </summary>
    [Fact]
    public async Task OccupyRoom_SeveralGroupsOverflowingRoom_Throws()
    {
        var smallType = mock.CreateAccommodationType("Двушка", capacity: 2);
        mock.ReInitProjectInfo();

        var first = CreateGroup(smallType, persons: 1);
        var second = CreateGroup(smallType, persons: 2);
        var room = mock.CreateEmptyRoom(smallType, "101");

        _ = await Should.ThrowAsync<JoinRpgInsufficientRoomSpaceException>(
            () => CreateService().OccupyRoom(RoomId(room), [GroupId(first), GroupId(second)]));

        // Операция целиком не сохранилась — ни первая группа, ни вторая в комнату не въехали.
        // (Откат самих трекаемых сущностей — дело EF, фейковый UnitOfWork его не изображает.)
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
        emailService.Sent.ShouldBeEmpty();
    }

    /// <summary>Ровно по вместимости несколько групп одной операцией заезжают.</summary>
    [Fact]
    public async Task OccupyRoom_SeveralGroupsExactlyFitting_Succeeds()
    {
        var smallType = mock.CreateAccommodationType("Двушка", capacity: 2);
        mock.ReInitProjectInfo();

        var first = CreateGroup(smallType, persons: 1);
        var second = CreateGroup(smallType, persons: 1);
        var room = mock.CreateEmptyRoom(smallType, "101");

        await CreateService().OccupyRoom(RoomId(room), [GroupId(first), GroupId(second)]);

        first.AccommodationId.ShouldBe(room.Id);
        second.AccommodationId.ShouldBe(room.Id);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Дефект 7 ADR018: <c>FirstOrDefaultAsync</c> без проверки на <c>null</c> давал NRE,
    /// а контроллер — 500.
    /// </summary>
    [Fact]
    public async Task OccupyRoom_UnknownRoom_ThrowsNotFound()
    {
        var group = CreateGroup(tent);

        _ = await Should.ThrowAsync<AccommodationRoomNotFoundException>(
            () => CreateService().OccupyRoom(new AccommodationRoomIdentification(ProjectId, 12345), [GroupId(group)]));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 7 ADR018.</summary>
    [Fact]
    public async Task OccupyRoom_UnknownGroup_ThrowsNotFound()
    {
        var room = mock.CreateEmptyRoom(tent, "101");

        _ = await Should.ThrowAsync<AccommodationGroupNotFoundException>(
            () => CreateService().OccupyRoom(
                RoomId(room), [new AccommodationRequestIdentification(ProjectId, 12345)]));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 2 ADR018: комната чужого проекта не должна находиться.</summary>
    [Fact]
    public async Task OccupyRoom_RoomFromAnotherProject_Throws()
    {
        var group = CreateGroup(tent);
        var room = mock.CreateEmptyRoom(tent, "101");

        _ = await Should.ThrowAsync<AccommodationRoomNotFoundException>(
            () => CreateService().OccupyRoom(AlienRoomId(room), [GroupId(group)]));

        group.AccommodationId.ShouldBeNull();
    }

    [Fact]
    public async Task UnOccupyGroup_EvictsSingleGroup()
    {
        var staying = CreateGroup(tent);
        var leaving = CreateGroup(tent);
        var room = mock.CreateRoom(staying, "101");
        leaving.Accommodation = room;
        leaving.AccommodationId = room.Id;
        room.Inhabitants.Add(leaving);

        await CreateService().UnOccupyGroup(GroupId(leaving));

        leaving.AccommodationId.ShouldBeNull();
        staying.AccommodationId.ShouldBe(room.Id);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
        journal.ShouldBe(["save", "email"]);
        emailService.Sent.ShouldHaveSingleItem().ShouldBeOfType<UnOccupyRoomEmail>();
    }

    /// <summary>Выселение идемпотентно: нерасселённую группу выселять нечего (ADR018, §1).</summary>
    [Fact]
    public async Task UnOccupyGroup_NotPlacedGroup_DoesNothing()
    {
        var group = CreateGroup(tent);

        await CreateService().UnOccupyGroup(GroupId(group));

        emailService.Sent.ShouldBeEmpty();
    }

    /// <summary>Дефект 7 ADR018.</summary>
    [Fact]
    public async Task UnOccupyGroup_UnknownGroup_ThrowsNotFound()
    {
        _ = await Should.ThrowAsync<AccommodationGroupNotFoundException>(
            () => CreateService().UnOccupyGroup(new AccommodationRequestIdentification(ProjectId, 12345)));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task UnOccupyRoom_EvictsEveryone()
    {
        var first = CreateGroup(tent);
        var second = CreateGroup(tent);
        var room = mock.CreateRoom(first, "101");
        second.Accommodation = room;
        second.AccommodationId = room.Id;
        room.Inhabitants.Add(second);

        await CreateService().UnOccupyRoom(RoomId(room));

        first.AccommodationId.ShouldBeNull();
        second.AccommodationId.ShouldBeNull();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
        // Комната одна — письмо одно, даже если жильцов было несколько.
        emailService.Sent.ShouldHaveSingleItem();
    }

    /// <summary>Дефект 7 ADR018.</summary>
    [Fact]
    public async Task UnOccupyRoom_UnknownRoom_ThrowsNotFound()
    {
        _ = await Should.ThrowAsync<AccommodationRoomNotFoundException>(
            () => CreateService().UnOccupyRoom(new AccommodationRoomIdentification(ProjectId, 12345)));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Дефект 3 ADR018: <c>UnOccupyRoomType</c> выселял каждую комнату отдельным
    /// <c>SaveChangesAsync</c>, и падение на пятой комнате оставляло четыре выселенными.
    /// Тип принадлежит одной категории, поэтому выселение типа — одна транзакция (ADR018, §1).
    /// </summary>
    [Fact]
    public async Task UnOccupyRoomType_SavesOnce_ForAllRooms()
    {
        var groups = new List<AccommodationRequest>();
        for (var i = 0; i < 4; i++)
        {
            var group = CreateGroup(tent);
            _ = mock.CreateRoom(group, $"10{i}");
            groups.Add(group);
        }

        await CreateService().UnOccupyRoomType(TentId);

        groups.ShouldAllBe(group => group.AccommodationId == null);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
        // Письма — по одному на комнату (RoomEmailBase несёт ровно одну комнату), но все после
        // единственного сохранения.
        emailService.Sent.Count.ShouldBe(4);
        journal.ShouldBe(["save", "email", "email", "email", "email"]);
    }

    /// <summary>Жильцов братского типа выселение типа не трогает.</summary>
    [Fact]
    public async Task UnOccupyRoomType_DoesNotTouchOtherTypes()
    {
        var hotel = mock.CreateAccommodationType("Отель", capacity: 4);
        mock.ReInitProjectInfo();

        var tentGroup = CreateGroup(tent);
        _ = mock.CreateRoom(tentGroup, "101");
        var hotelGroup = CreateGroup(hotel);
        var hotelRoom = mock.CreateRoom(hotelGroup, "201");

        await CreateService().UnOccupyRoomType(TentId);

        tentGroup.AccommodationId.ShouldBeNull();
        hotelGroup.AccommodationId.ShouldBe(hotelRoom.Id);
    }

    [Fact]
    public async Task UnOccupyRoomType_WithoutPermission_Throws()
    {
        var group = CreateGroup(tent);
        _ = mock.CreateRoom(group, "101");

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).UnOccupyRoomType(TentId));

        group.AccommodationId.ShouldNotBeNull();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task UnOccupyRoomType_UnknownType_ThrowsNotFound()
        => _ = await Should.ThrowAsync<AccommodationTypeNotFoundException>(
            () => CreateService().UnOccupyRoomType(new AccommodationTypeIdentification(ProjectId, 12345)));

    /// <summary>
    /// Выселение проекта пересекает пулы и остаётся циклом по категориям — по транзакции на
    /// каждую (ADR018, §1). Это сознательно оставшаяся половина дефекта 3.
    /// </summary>
    [Fact]
    public async Task UnOccupyAllRooms_SavesOncePerCategory()
    {
        var hotel = mock.CreateAccommodationType("Отель", capacity: 4);
        mock.ReInitProjectInfo();

        var tentGroup = CreateGroup(tent);
        _ = mock.CreateRoom(tentGroup, "101");
        var hotelGroup = CreateGroup(hotel);
        _ = mock.CreateRoom(hotelGroup, "201");

        await CreateService().UnOccupyAllRooms(ProjectId);

        tentGroup.AccommodationId.ShouldBeNull();
        hotelGroup.AccommodationId.ShouldBeNull();
        unitOfWork.SaveChangesCallCount.ShouldBe(2);
        emailService.Sent.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UnOccupyAllRooms_WithoutPermission_Throws()
    {
        var group = CreateGroup(tent);
        _ = mock.CreateRoom(group, "101");

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).UnOccupyAllRooms(ProjectId));

        group.AccommodationId.ShouldNotBeNull();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 4 ADR018.</summary>
    [Fact]
    public async Task UnOccupyGroup_InArchivedProject_Throws()
    {
        var group = CreateGroup(tent);
        _ = mock.CreateRoom(group, "101");
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().UnOccupyGroup(GroupId(group)));

        group.AccommodationId.ShouldNotBeNull();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 4 ADR018.</summary>
    [Fact]
    public async Task UnOccupyRoom_InArchivedProject_Throws()
    {
        var group = CreateGroup(tent);
        var room = mock.CreateRoom(group, "101");
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().UnOccupyRoom(RoomId(room)));

        group.AccommodationId.ShouldNotBeNull();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 4 ADR018.</summary>
    [Fact]
    public async Task UnOccupyRoomType_InArchivedProject_Throws()
    {
        var group = CreateGroup(tent);
        _ = mock.CreateRoom(group, "101");
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().UnOccupyRoomType(TentId));

        group.AccommodationId.ShouldNotBeNull();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 4 ADR018.</summary>
    [Fact]
    public async Task UnOccupyAllRooms_InArchivedProject_Throws()
    {
        var group = CreateGroup(tent);
        _ = mock.CreateRoom(group, "101");
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().UnOccupyAllRooms(ProjectId));

        group.AccommodationId.ShouldNotBeNull();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }
}
