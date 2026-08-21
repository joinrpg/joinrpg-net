using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Accommodation;

/// <summary>
/// Вариант проживания, доступный игроку для выбора.
/// </summary>
/// <param name="TypeId">Идентификатор типа проживания</param>
/// <param name="Name">Название («Палатка», «Домик на четверых»…)</param>
/// <param name="Capacity">Сколько человек помещается в номере</param>
/// <param name="Cost">Стоимость</param>
/// <param name="DescriptionHtml">Описание, уже отрисованное из Markdown</param>
public record AccommodationTypeViewModel(
    AccommodationTypeIdentification TypeId,
    string Name,
    int Capacity,
    int Cost,
    string DescriptionHtml)
{
    // Это нужно, потому что MarkupString не умеет нормально десериализоваться из JSON
    [JsonIgnore]
    public MarkupString Description { get; } = new(DescriptionHtml);
}

/// <summary>
/// Состояние диалога выбора типа проживания.
/// </summary>
/// <param name="Types">Доступные игроку варианты</param>
/// <param name="SelectedTypeId">Что выбрано сейчас. <c>null</c> — проживание ещё не выбрано</param>
/// <param name="RoomAssigned">Комната уже назначена — смена типа выселит игрока</param>
/// <param name="HasNeighbours">Есть соседи — смена типа отменит договорённости о совместном проживании</param>
public record AccommodationTypeChoiceViewModel(
    IReadOnlyCollection<AccommodationTypeViewModel> Types,
    AccommodationTypeIdentification? SelectedTypeId,
    bool RoomAssigned,
    bool HasNeighbours);

/// <summary>
/// Выбор игроком типа проживания.
/// </summary>
public interface IAccommodationTypeClient
{
    /// <summary>Варианты проживания, доступные этой заявке</summary>
    Task<AccommodationTypeChoiceViewModel> GetAccommodationTypes(ClaimIdentification claimId);

    /// <summary>Выбрать тип проживания</summary>
    Task SetAccommodationType(ClaimIdentification claimId, AccommodationTypeIdentification typeId);
}
