namespace JoinRpg.Services.Interfaces.ProjectMetadata;

/// <summary>
/// Параметры создаваемого типа проживания.
/// </summary>
/// <param name="Name">Название типа, как его видит игрок</param>
/// <param name="Description">Описание типа проживания</param>
/// <param name="Cost">Стоимость проживания для одного игрока</param>
/// <param name="Capacity">Сколько игроков помещается в одну комнату этого типа</param>
/// <param name="IsPlayerSelectable">Может ли игрок сам выбрать этот тип в своей заявке</param>
public record AccommodationTypeCreateRequest(
    string Name,
    MarkdownString Description,
    int Cost,
    int Capacity,
    bool IsPlayerSelectable);

/// <summary>
/// Новые параметры существующего типа проживания.
/// </summary>
/// <param name="Name">Название типа, как его видит игрок</param>
/// <param name="Description">Описание типа проживания</param>
/// <param name="Cost">Стоимость проживания для одного игрока</param>
/// <param name="Capacity">Сколько игроков помещается в одну комнату этого типа</param>
/// <param name="IsPlayerSelectable">Может ли игрок сам выбрать этот тип в своей заявке</param>
public record AccommodationTypeUpdateRequest(
    string Name,
    MarkdownString Description,
    int Cost,
    int Capacity,
    bool IsPlayerSelectable);

/// <summary>
/// Типы проживания проекта (палатка, домик, номер в отеле…) — настройка мастера, часть метаданных
/// проекта (ADR015), поэтому изменения идут через <c>ProjectPropsService</c> (ADR009).
/// Комнаты и заселение — не настройка, они остаются в <see cref="IAccommodationService"/>.
/// </summary>
public interface IAccommodationTypeService
{
    /// <summary>
    /// Создаёт новый тип проживания в проекте.
    /// </summary>
    Task<AccommodationTypeIdentification> CreateAccommodationType(
        ProjectIdentification projectId,
        AccommodationTypeCreateRequest request);

    /// <summary>
    /// Изменяет параметры типа проживания.
    /// </summary>
    Task UpdateAccommodationType(
        AccommodationTypeIdentification accommodationTypeId,
        AccommodationTypeUpdateRequest request);

    /// <summary>
    /// Удаляет тип проживания.
    /// </summary>
    /// <exception cref="JoinRpg.Domain.RoomIsOccupiedException">
    /// В одной из комнат этого типа кто-то живёт — сначала нужно выселить.
    /// </exception>
    Task DeleteAccommodationType(AccommodationTypeIdentification accommodationTypeId);
}
