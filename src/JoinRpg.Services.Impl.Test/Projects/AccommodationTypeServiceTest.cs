using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Services.Impl.Projects.Metadata;
using JoinRpg.Services.Interfaces.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Projects;

/// <summary>
/// Тесты <see cref="AccommodationTypeService"/>: создание, изменение и удаление типа проживания
/// как настройки проекта (ADR015). Результат проверяется по пересобранному
/// <see cref="ProjectInfo"/> (ADR009), а не по EF-сущности.
/// </summary>
public class AccommodationTypeServiceTest : ProjectMetadataServiceTestBase
{
    /// <summary>Комната, которую «видит» репозиторий как занятую; <c>null</c> — все свободны.</summary>
    private ProjectAccommodation? occupiedRoom;

    private AccommodationTypeService CreateService(int? currentUserId = null, bool isAdmin = false)
    {
        var currentUser = CreateCurrentUser(currentUserId, isAdmin);
        return new AccommodationTypeService(
            CreatePropsService(currentUser),
            new FakeAccommodationRepository(() => occupiedRoom));
    }

    private AccommodationTypeIdentification AddAccommodationType(string name = "Палатка")
    {
        var entity = mock.CreateAccommodationType(name);
        mock.ReInitProjectInfo();
        return new AccommodationTypeIdentification(ProjectId, entity.Id);
    }

    [Fact]
    public async Task CreateAccommodationType_AddsTypeToProjectSettings()
    {
        await CreateService().CreateAccommodationType(
            ProjectId,
            new AccommodationTypeCreateRequest("Домик", new MarkdownString("Тёплый"), Cost: 1500, Capacity: 4, IsPlayerSelectable: true));

        var created = Result.AccommodationSettings.Types.ShouldHaveSingleItem();
        created.Name.ShouldBe("Домик");
        created.Description.Value.ShouldBe("Тёплый");
        created.Cost.ShouldBe(1500);
        created.Capacity.ShouldBe(4);
        created.IsPlayerSelectable.ShouldBeTrue();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAccommodationType_WithoutPermission_Throws()
    {
        // Игрок (mock.Player) не входит в ACL проекта
        await Should.ThrowAsync<NoAccessToProjectException>(() => CreateService(mock.Player.UserId).CreateAccommodationType(
            ProjectId,
            new AccommodationTypeCreateRequest("Домик", new MarkdownString(""), Cost: 0, Capacity: 1, IsPlayerSelectable: true)));

        mock.AccommodationTypes.ShouldBeEmpty();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateAccommodationType_ChangesSettings()
    {
        var id = AddAccommodationType();

        await CreateService().UpdateAccommodationType(
            id,
            new AccommodationTypeUpdateRequest("Люкс", new MarkdownString("С душем"), Cost: 9000, Capacity: 2, IsPlayerSelectable: false));

        var updated = Result.AccommodationSettings.Types.ShouldHaveSingleItem();
        updated.Name.ShouldBe("Люкс");
        updated.Description.Value.ShouldBe("С душем");
        updated.Cost.ShouldBe(9000);
        updated.Capacity.ShouldBe(2);
        updated.IsPlayerSelectable.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateAccommodationType_WithoutPermission_Throws()
    {
        var id = AddAccommodationType();

        await Should.ThrowAsync<NoAccessToProjectException>(() => CreateService(mock.Player.UserId).UpdateAccommodationType(
            id,
            new AccommodationTypeUpdateRequest("Люкс", new MarkdownString(""), Cost: 9000, Capacity: 2, IsPlayerSelectable: false)));

        mock.AccommodationTypes.ShouldHaveSingleItem().Name.ShouldBe("Палатка");
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteAccommodationType_RemovesTypeFromProjectSettings()
    {
        var id = AddAccommodationType();

        await CreateService().DeleteAccommodationType(id);

        Result.AccommodationSettings.Types.ShouldBeEmpty();
        mock.AccommodationTypes.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAccommodationType_WithoutPermission_Throws()
    {
        var id = AddAccommodationType();

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).DeleteAccommodationType(id));

        mock.AccommodationTypes.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteAccommodationType_WithOccupiedRoom_Throws()
    {
        var id = AddAccommodationType();
        var accommodationType = mock.AccommodationTypes.Single();
        var request = mock.CreateAccommodationRequest(accommodationType);
        occupiedRoom = mock.CreateRoom(request);

        _ = await Should.ThrowAsync<RoomIsOccupiedException>(() => CreateService().DeleteAccommodationType(id));

        mock.AccommodationTypes.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Занятость комнат — оперативные данные вне метаданных проекта, сервис берёт их отдельным
    /// запросом. Фейк отдаёт то, что задал тест.
    /// </summary>
    private sealed class FakeAccommodationRepository(Func<ProjectAccommodation?> occupiedRoom) : IAccommodationRepository
    {
        public Task<ProjectAccommodation?> GetOccupiedRoomOfType(AccommodationTypeIdentification accommodationTypeId)
            => Task.FromResult(occupiedRoom());

        public Task<IReadOnlyCollection<ProjectAccommodationType>> GetAccommodationForProject(int projectId)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<ClaimAccommodationInfoRow>> GetClaimAccommodationReport(int project)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<RoomTypeInfoRow>> GetRoomTypesForProject(int project)
            => throw new NotSupportedException();

        public Task<ProjectAccommodationType> GetRoomTypeById(int roomTypeId)
            => throw new NotSupportedException();
    }
}
