using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters.Claims.Accommodation;

/// <summary>
/// Приглашение к совместному проживанию отправить нельзя. Причина отказа приходит в
/// <paramref name="message"/> и показывается игроку.
/// </summary>
public class AccommodationInviteNotAllowedException(ProjectIdentification projectId, string message)
    : JoinRpgProjectException(projectId, message);

/// <summary>
/// В проекте нет типа проживания с таким идентификатором.
/// </summary>
/// <remarks>
/// Аналог <c>JoinRpgEntityNotFoundException</c> из <c>JoinRpg.Data.Interfaces</c>: тот живёт слоем
/// выше и отсюда недоступен, а бросать <see cref="KeyNotFoundException"/> из доменной модели
/// нельзя — это исключение про коллекции, а не про предметную область.
/// </remarks>
public class AccommodationTypeNotFoundException(AccommodationTypeIdentification accommodationTypeId)
    : JoinRpgProjectException(
        accommodationTypeId.ProjectId,
        $"Не найден тип проживания с ID={accommodationTypeId}")
{
    public AccommodationTypeIdentification AccommodationTypeId { get; } = accommodationTypeId;
}

/// <summary>
/// В плане поселения нет комнаты с таким идентификатором.
/// </summary>
public class AccommodationRoomNotFoundException(AccommodationRoomIdentification roomId)
    : JoinRpgProjectException(roomId.ProjectId, $"Не найдена комната с ID={roomId}")
{
    public AccommodationRoomIdentification RoomId { get; } = roomId;
}

/// <summary>
/// В плане поселения нет группы проживающих с таким идентификатором.
/// </summary>
public class AccommodationGroupNotFoundException(AccommodationRequestIdentification groupId)
    : JoinRpgProjectException(groupId.ProjectId, $"Не найдена группа проживающих с ID={groupId}")
{
    public AccommodationRequestIdentification GroupId { get; } = groupId;
}
