namespace JoinRpg.Services.Interfaces.ProjectMetadata;

/// <summary>
/// Параметры типа проживания — те же и при создании, и при изменении.
/// </summary>
/// <param name="Name">Название типа, как его видит игрок</param>
/// <param name="Description">Описание типа проживания</param>
/// <param name="Cost">Стоимость проживания для одного игрока</param>
/// <param name="Capacity">Сколько игроков помещается в одну комнату этого типа</param>
/// <param name="IsPlayerSelectable">Может ли игрок сам выбрать этот тип в своей заявке</param>
public record AccommodationTypeRequest(
    string Name,
    MarkdownString Description,
    int Cost,
    int Capacity,
    bool IsPlayerSelectable);

/// <summary>
/// Из какой категории комнат будет селиться новый тип проживания (ADR020).
/// </summary>
public abstract record RoomCategorySelection;

/// <summary>Завести под тип новую категорию комнат с этим именем.</summary>
public sealed record NewRoomCategory(string Name) : RoomCategorySelection;

/// <summary>Селить тип из уже существующей категории — вместе с её типами.</summary>
public sealed record ExistingRoomCategory(RoomCategoryIdentification Id) : RoomCategorySelection;

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
    /// <param name="roomCategory">
    /// Категория комнат типа. Без неё тип получает новую категорию с собственным именем.
    /// </param>
    /// <exception cref="RoomCategoryNotFoundException">Выбранной категории в проекте нет</exception>
    Task<AccommodationTypeIdentification> CreateAccommodationType(
        ProjectIdentification projectId,
        AccommodationTypeRequest request,
        RoomCategorySelection? roomCategory = null);

    /// <summary>
    /// Изменяет параметры типа проживания.
    /// </summary>
    Task UpdateAccommodationType(
        AccommodationTypeIdentification accommodationTypeId,
        AccommodationTypeRequest request);

    /// <summary>
    /// Удаляет тип проживания. Последний тип категории удаляет и её вместе с комнатами; если у
    /// категории есть другие типы, комнаты остаются им (ADR020).
    /// </summary>
    /// <exception cref="AccommodationTypeIsOccupiedException">
    /// Кто-то из купивших этот тип уже расселён — сначала нужно выселить.
    /// </exception>
    Task DeleteAccommodationType(AccommodationTypeIdentification accommodationTypeId);
}
