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
