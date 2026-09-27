using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.DomainTypes.ProjectMetadata.Accommodation;

/// <summary>
/// Тип проживания в проекте (палатка, домик, номер в отеле…) — настройка мастера, часть
/// метаданных проекта (ADR015).
/// </summary>
/// <param name="Id">Идентификатор типа проживания</param>
/// <param name="Name">Название типа, как его видит игрок</param>
/// <param name="Description">Описание типа проживания</param>
/// <param name="Cost">Стоимость проживания для одного игрока</param>
/// <param name="Capacity">Сколько игроков помещается в одну комнату этого типа</param>
/// <param name="IsPlayerSelectable">Может ли игрок сам выбрать этот тип в своей заявке</param>
/// <remarks>
/// Поля <c>IsInfinite</c> и <c>IsAutoFilledAccommodation</c> EF-сущности сюда не переносятся:
/// они помечены «not implemented yet» и нигде не читаются.
/// </remarks>
public record AccommodationTypeInfo(
    AccommodationTypeIdentification Id,
    string Name,
    MarkdownString Description,
    int Cost,
    int Capacity,
    bool IsPlayerSelectable);

/// <summary>
/// Настройки проживания проекта: включён ли модуль и какие типы проживания заведены.
/// </summary>
/// <param name="Enabled">Включён ли модуль проживания в проекте</param>
/// <param name="Types">Все типы проживания проекта</param>
public record ProjectAccommodationSettings(
    bool Enabled,
    IReadOnlyCollection<AccommodationTypeInfo> Types)
{
    /// <summary>Тип проживания по идентификатору</summary>
    /// <exception cref="KeyNotFoundException">Типа проживания с таким идентификатором нет</exception>
    public AccommodationTypeInfo GetTypeById(AccommodationTypeIdentification id)
    {
        return GetTypeByIdOrDefault(id)
            ?? throw new KeyNotFoundException("Не найден тип проживания с ID=" + id);
    }

    /// <summary>
    /// Тип проживания по идентификатору или <c>null</c>, если такого нет. Для мест, где
    /// идентификатор приходит параметром фильтра и промах — это 404, а не ошибка.
    /// </summary>
    public AccommodationTypeInfo? GetTypeByIdOrDefault(AccommodationTypeIdentification id)
        => Types.SingleOrDefault(t => t.Id == id);

    /// <summary>Типы, доступные игроку для самостоятельного выбора</summary>
    public IReadOnlyCollection<AccommodationTypeInfo> PlayerSelectableTypes
        => [.. Types.Where(t => t.IsPlayerSelectable)];
}
