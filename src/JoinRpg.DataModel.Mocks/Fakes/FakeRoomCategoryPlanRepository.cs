using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Загрузчик плана поселения поверх мока — общий для всех тестовых проектов.
/// </summary>
/// <remarks>
/// Собирает план так же, как настоящий загрузчик: типы — экземпляры из метаданных мока (инвариант
/// ссылочного равенства ADR013), комнаты — по их категории, группы — все
/// заявки на поселение типов категории, включая нерасселённые.
/// </remarks>
public sealed class FakeRoomCategoryPlanRepository(MockedProject mock) : IRoomCategoryPlanRepository
{
    public Task<RoomCategoryPlan?> GetPlanForTypeOrDefault(AccommodationTypeIdentification typeId)
    {
        var projectInfo = mock.ProjectInfo;
        var type = projectInfo.AccommodationSettings.GetTypeByIdOrDefault(typeId);
        return Task.FromResult(type is null ? null : Build(projectInfo, type.RoomCategoryId));
    }

    public Task<IReadOnlyCollection<RoomCategoryPlan>> GetAllPlans(ProjectIdentification projectId)
    {
        var projectInfo = mock.ProjectInfo;
        return Task.FromResult<IReadOnlyCollection<RoomCategoryPlan>>(
            [.. projectInfo.AccommodationSettings.Types
                .Select(type => type.RoomCategoryId)
                .Distinct()
                .Select(categoryId => Build(projectInfo, categoryId))]);
    }

    private RoomCategoryPlan Build(ProjectInfo projectInfo, RoomCategoryIdentification categoryId)
    {
        var projectId = projectInfo.ProjectId;
        var types = projectInfo.AccommodationSettings.Types
            .Where(type => type.RoomCategoryId == categoryId)
            .ToArray();
        var typeIds = types.Select(type => type.Id.AccommodationTypeId).ToHashSet();

        var groups = mock.AccommodationRequests
            .Where(request => typeIds.Contains(request.AccommodationTypeId))
            .Select(request => new AccommodationGroupInfo(
                new AccommodationRequestIdentification(projectId, request.Id),
                new AccommodationTypeIdentification(projectId, request.AccommodationTypeId),
                request.AccommodationId is { } roomId ? new AccommodationRoomIdentification(projectId, roomId) : null,
                [.. request.Subjects.Select(claim => claim.GetId())]))
            .ToArray();

        // Комната принадлежит категории (ADR020).
        var rooms = mock.Rooms
            .Where(room => room.RoomCategoryId == categoryId.RoomCategoryId)
            .Select(room => new RoomInfo(
                new AccommodationRoomIdentification(projectId, room.Id),
                room.Name,
                [.. groups.Where(group => group.RoomId?.RoomId == room.Id)]))
            .ToArray();

        return new RoomCategoryPlan(categoryId, projectInfo, types, rooms, groups);
    }
}
