using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Dal.Impl.Repositories.Accommodation;

/// <summary>
/// Чистое преобразование плоской проекции из БД в доменный агрегат поселения (ADR018).
/// Вынесено из загрузчика, чтобы покрываться юнит-тестами без базы.
/// </summary>
internal static class RoomCategoryPlanMapper
{
    public static RoomCategoryPlan Map(RoomCategoryPlanRow row, ProjectInfo projectInfo)
    {
        var projectId = projectInfo.ProjectId;
        var categoryId = new RoomCategoryIdentification(projectId, row.RoomCategoryId);

        // Типы проживания не копируются, а берутся из метаданных проекта: единственный источник
        // правды о типе — ProjectInfo, и конструктор плана проверяет это через ReferenceEquals
        // (ADR018, §3). Типов у категории может быть несколько (ADR020).
        var accommodationTypes = projectInfo.AccommodationSettings.GetTypesOfCategory(categoryId);

        var groups = row.Groups
            .Select(group => new AccommodationGroupInfo(
                new AccommodationRequestIdentification(projectId, group.GroupId),
                new AccommodationTypeIdentification(projectId, group.AccommodationTypeId),
                group.RoomId is int roomId ? new AccommodationRoomIdentification(projectId, roomId) : null,
                [.. group.SubjectClaimIds.Select(claimId => new ClaimIdentification(projectId, claimId))]))
            .ToArray();

        var groupsByRoom = groups
            .Where(group => group.RoomId is not null)
            .ToLookup(group => group.RoomId!);

        var rooms = row.Rooms
            .Select(room => MapRoom(room, projectId, groupsByRoom))
            .ToArray();

        return new RoomCategoryPlan(
            categoryId,
            projectInfo,
            accommodationTypes,
            rooms,
            groups);
    }

    private static RoomInfo MapRoom(
        RoomCategoryPlanRoomRow room,
        ProjectIdentification projectId,
        ILookup<AccommodationRoomIdentification, AccommodationGroupInfo> groupsByRoom)
    {
        var roomId = new AccommodationRoomIdentification(projectId, room.RoomId);
        return new RoomInfo(roomId, room.Name, [.. groupsByRoom[roomId]]);
    }
}
