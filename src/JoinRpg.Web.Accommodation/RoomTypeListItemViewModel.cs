using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Models.Accommodation;

/// <summary>
/// Оперативные счётчики по одному типу проживания для страницы «Поселение».
/// </summary>
/// <remarks>
/// В метаданные проекта они не входят: занятость считает SQL-сводка
/// (<c>IAccommodationRepository.GetRoomTypesForProject</c>), а <paramref name="PaidCount"/> —
/// расчёт баланса по нерасселённым заявкам. Отдельный тип, а не ряд репозитория, потому что
/// вью-модель лежит в браузерной библиотеке и на DAL не смотрит.
/// </remarks>
/// <param name="Occupied">Сколько мест этого типа занято жильцами</param>
/// <param name="RoomsCount">Сколько комнат этого типа заведено</param>
/// <param name="ApprovedClaims">Сколько заявок выбрало этот тип проживания — и расселённых, и нет</param>
/// <param name="FullyFreeRoomsCount">Комнаты, в которых не живёт никто</param>
/// <param name="FullyOccupiedRoomsCount">Комнаты, занятые под вместимость</param>
/// <param name="PaidCount">Сколько нерасселённых заявок оплачено полностью</param>
public record RoomTypeOccupancySummary(
    int Occupied,
    int RoomsCount,
    int ApprovedClaims,
    int FullyFreeRoomsCount,
    int FullyOccupiedRoomsCount,
    int PaidCount);

/// <summary>
/// Строка таблицы типов проживания на странице «Поселение».
/// </summary>
/// <remarks>
/// Настройки типа берутся из метаданных проекта (ADR015), занятость — из оперативной сводки
/// <see cref="RoomTypeOccupancySummary"/>.
/// </remarks>
public class RoomTypeListItemViewModel : RoomTypeViewModelBase
{
    [DisplayName("Проживает")]
    public int Occupied { get; }

    public int PendingRequests { get; }

    public override int RoomsCount { get; }

    public int FullyFreeRoomsCount { get; }
    public int FullyOccupiedRoomsCount { get; }
    public int PartialRoomsCount { get; }
    public int PartialFreeSeats { get; }
    public int PartialOccupiedSeats { get; }

    public int PaidCount { get; }
    public int AcceptedNotPaidCount { get; }

    public int ApprovedClaims { get; }
    public int FreeCapacity { get; }

    /// <param name="typeInfo">Настройки типа проживания из метаданных проекта (ADR015)</param>
    /// <param name="descriptionView">
    /// Описание типа проживания, уже отрендеренное из Markdown вызывающей стороной
    /// (рендерер markdown живёт на сервере, см. <see cref="RoomTypeViewModelBase.DescriptionView"/>).
    /// </param>
    /// <param name="occupancy">Оперативные счётчики по этому типу проживания</param>
    /// <param name="currentUserId">
    /// Пользователь, который смотрит страницу: по нему считаются права
    /// <see cref="RoomTypeViewModelBase.CanManageRooms"/> и
    /// <see cref="RoomTypeViewModelBase.CanAssignRooms"/>.
    /// </param>
    /// <remarks>
    /// <c>IsInfinite</c> и <c>IsAutoFilledAccommodation</c> здесь не заполняются: в метаданных их
    /// нет по решению ADR015 — в EF-сущности оба помечены «not implemented yet», и путь сохранения
    /// типа проживания (<c>AccommodationTypeRequest</c>) их не пишет.
    /// </remarks>
    public RoomTypeListItemViewModel(
        AccommodationTypeInfo typeInfo,
        MarkupString descriptionView,
        RoomTypeOccupancySummary occupancy,
        UserIdentification currentUserId,
        ProjectInfo projectInfo)
    {
        Id = typeInfo.Id.AccommodationTypeId;
        Cost = typeInfo.Cost;
        Name = typeInfo.Name;
        Capacity = typeInfo.Capacity;
        IsPlayerSelectable = typeInfo.IsPlayerSelectable;
        DescriptionHtml = descriptionView.Value;

        ProjectId = projectInfo.ProjectId.Value;
        CanManageRooms = projectInfo.HasMasterAccess(currentUserId, Permission.CanManageAccommodation);
        CanAssignRooms = projectInfo.HasMasterAccess(currentUserId, Permission.CanSetPlayersAccommodations);

        Occupied = occupancy.Occupied;
        RoomsCount = occupancy.RoomsCount;
        ApprovedClaims = occupancy.ApprovedClaims;

        FreeCapacity = TotalCapacity - Occupied;
        PendingRequests = ApprovedClaims - Occupied;

        FullyFreeRoomsCount = occupancy.FullyFreeRoomsCount;
        FullyOccupiedRoomsCount = occupancy.FullyOccupiedRoomsCount;
        PartialRoomsCount = RoomsCount - FullyFreeRoomsCount - FullyOccupiedRoomsCount;
        PartialFreeSeats = FreeCapacity - (Capacity * FullyFreeRoomsCount);
        PartialOccupiedSeats = Occupied - (Capacity * FullyOccupiedRoomsCount);

        PaidCount = occupancy.PaidCount;
        AcceptedNotPaidCount = PendingRequests - PaidCount;
    }

    // Подписи для tooltip-ов над отдельными цифрами формул в разметке (Index.cshtml, _RoomTypeDetails.cshtml)
    public const string CapacityTooltip = "это вместимость номера";
    public const string FullyFreeRoomsTooltip = "это кол-во полностью свободных номеров";
    public const string PartialFreeSeatsTooltip = "количество свободных мест в частично занятых номерах";
    public const string TotalFreeTooltip = "всего свободных мест в этой категории";
    public const string FullyOccupiedRoomsTooltip = "это кол-во полностью занятых номеров";
    public const string PartialOccupiedSeatsTooltip = "количество занятых мест в частично занятых номерах";
    public const string TotalOccupiedTooltip = "всего занятых мест в этой категории";
    public const string PaidTooltip = "оплаченных";
    public const string AcceptedNotPaidTooltip = "принятых, не оплаченных";
    public const string TotalUnsettledTooltip = "всего";
}
