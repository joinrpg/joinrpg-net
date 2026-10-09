namespace JoinRpg.Web.Accommodation.Rooms;

/// <summary>Тело запроса «добавить комнаты»</summary>
/// <param name="TypeId">Тип проживания, на странице которого добавляют комнаты</param>
/// <param name="RoomNames">Строка из поля ввода: имена через запятую, диапазоны через дефис</param>
public record AddRoomsRequest(AccommodationTypeIdentification TypeId, string RoomNames);

/// <summary>Тело запроса «переименовать комнату»</summary>
public record RenameRoomRequest(AccommodationRoomIdentification RoomId, string Name);

/// <summary>Тело запроса «заселить группы в комнату»</summary>
public record OccupyRoomRequest(
    AccommodationRoomIdentification RoomId,
    IReadOnlyCollection<AccommodationRequestIdentification> GroupIds);
