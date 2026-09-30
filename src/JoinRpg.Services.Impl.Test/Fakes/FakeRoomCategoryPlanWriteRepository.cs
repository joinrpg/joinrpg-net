using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Write-репозиторий агрегата поселения (ADR018) поверх <see cref="MockedProject"/>: отдаёт
/// трекаемые EF-сущности комнат вместе с согласованным доменным снимком
/// <see cref="RoomCategoryPlan"/>, собранным из того же мока.
/// </summary>
internal sealed class FakeRoomCategoryPlanWriteRepository(MockedProject mock) : IRoomCategoryPlanWriteRepository
{
    public Task<IRoomCategoryPlanUpdateHandle> LoadPlanForUpdate(
        ProjectInfo projectInfo,
        RoomCategoryIdentification categoryId)
    {
        // Фильтр по проекту повторяет боевой запрос: категорию чужого проекта не найти.
        var category = mock.AccommodationTypes.SingleOrDefault(
                type => type.Id == categoryId.RoomCategoryId && type.ProjectId == categoryId.ProjectId.Value)
            ?? throw new JoinRpgEntityNotFoundException(categoryId.RoomCategoryId, "room category");

        return Task.FromResult<IRoomCategoryPlanUpdateHandle>(new Handle(mock, projectInfo, category));
    }

    public Task<IRoomCategoryPlanUpdateHandle> LoadPlanForRoomUpdate(
        ProjectInfo projectInfo,
        AccommodationRoomIdentification roomId)
    {
        // Фильтр по проекту здесь существен: он закрывает дефект 2 ADR018.
        var room = mock.Rooms.SingleOrDefault(
                r => r.Id == roomId.RoomId && r.ProjectId == roomId.ProjectId.Value)
            ?? throw new AccommodationRoomNotFoundException(roomId);

        return LoadPlanForUpdate(
            projectInfo,
            new RoomCategoryIdentification(roomId.ProjectId, room.AccommodationTypeId));
    }

    private sealed class Handle : IRoomCategoryPlanUpdateHandle
    {
        private readonly MockedProject mock;

        public Handle(MockedProject mock, ProjectInfo projectInfo, ProjectAccommodationType category)
        {
            this.mock = mock;
            Category = category;

            // Снимок метаданных приходит снаружи, как и в бою: конструктор плана требует ровно
            // того же экземпляра типов (ADR018, §3).
            ProjectInfo = projectInfo;

            var projectId = ProjectInfo.ProjectId;
            var categoryId = new RoomCategoryIdentification(projectId, category.Id);

            Rooms = mock.Rooms
                .Where(room => room.AccommodationTypeId == category.Id)
                .ToDictionary(room => new AccommodationRoomIdentification(projectId, room.Id));

            // Группы нужны только доменному снимку: трекаемыми их хэндл больше не отдаёт —
            // управление комнатами их не меняет.
            var groups = mock.AccommodationRequests
                .Where(group => group.AccommodationTypeId == category.Id)
                .ToArray();

            Plan = BuildPlan(categoryId, ProjectInfo, category, Rooms.Values, groups);
        }

        public ProjectInfo ProjectInfo { get; }

        public RoomCategoryPlan Plan { get; }

        public ProjectAccommodationType Category { get; }

        public IReadOnlyDictionary<AccommodationRoomIdentification, ProjectAccommodation> Rooms { get; }

        /// <summary>Всё, что сервис добавил в контекст, в порядке добавления.</summary>
        public List<object> Added { get; } = [];

        /// <summary>Всё, что сервис удалил из контекста, в порядке удаления.</summary>
        public List<object> Removed { get; } = [];

        public void Add(object entity)
        {
            Added.Add(entity);
            if (entity is ProjectAccommodation room)
            {
                mock.Rooms.Add(room);
            }
        }

        public void Remove(object entity)
        {
            Removed.Add(entity);
            if (entity is ProjectAccommodation room)
            {
                _ = mock.Rooms.Remove(room);
                _ = room.ProjectAccommodationType.ProjectAccommodations.Remove(room);
            }
        }

        private static RoomCategoryPlan BuildPlan(
            RoomCategoryIdentification categoryId,
            ProjectInfo projectInfo,
            ProjectAccommodationType category,
            IEnumerable<ProjectAccommodation> rooms,
            IEnumerable<AccommodationRequest> groups)
        {
            var projectId = categoryId.ProjectId;

            var groupInfos = groups
                .Select(group => new AccommodationGroupInfo(
                    new AccommodationRequestIdentification(projectId, group.Id),
                    new AccommodationTypeIdentification(projectId, group.AccommodationTypeId),
                    group.AccommodationId is int roomId
                        ? new AccommodationRoomIdentification(projectId, roomId)
                        : null,
                    [.. group.Subjects.Select(claim => new ClaimIdentification(projectId, claim.ClaimId))]))
                .ToArray();

            var groupsByRoom = groupInfos
                .Where(group => group.RoomId is not null)
                .ToLookup(group => group.RoomId!);

            var roomInfos = rooms
                .Select(room =>
                {
                    var roomId = new AccommodationRoomIdentification(projectId, room.Id);
                    return new RoomInfo(roomId, room.Name, [.. groupsByRoom[roomId]]);
                })
                .ToArray();

            // Типы берутся из метаданных, а не пересобираются: конструктор плана проверяет
            // ссылочное равенство (ADR018, §3).
            var types = projectInfo.AccommodationSettings.Types
                .Where(type => type.RoomCategoryId == categoryId)
                .ToArray();

            return new RoomCategoryPlan(categoryId, projectInfo, types, category.Capacity, roomInfos, groupInfos);
        }
    }
}
