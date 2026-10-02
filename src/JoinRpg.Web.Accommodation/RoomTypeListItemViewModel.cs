using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Models.Accommodation;

/// <summary>
/// Оперативная занятость одного типа проживания для страницы «Поселение»: счётчики и вся
/// арифметика формул, которые по ним показывает разметка.
/// </summary>
/// <remarks>
/// В метаданные проекта это не входит: занятость считает SQL-сводка
/// (<c>IAccommodationRepository.GetRoomTypesForProject</c>), а <paramref name="PaidCount"/> —
/// расчёт баланса по нерасселённым заявкам. Отдельный тип, а не ряд репозитория, потому что
/// вью-модель лежит в браузерной библиотеке и на DAL не смотрит.
/// </remarks>
/// <param name="Capacity">
/// Вместимость одной комнаты — настройка типа из метаданных (ADR015). Нужна здесь, чтобы
/// раскладывать места по полностью и частично занятым комнатам.
/// </param>
/// <param name="Occupied">Сколько мест этого типа занято жильцами</param>
/// <param name="RoomsCount">Сколько комнат этого типа заведено</param>
/// <param name="ApprovedClaims">Сколько заявок выбрало этот тип проживания — и расселённых, и нет</param>
/// <param name="FullyFreeRoomsCount">Комнаты, в которых не живёт никто</param>
/// <param name="FullyOccupiedRoomsCount">Комнаты, занятые под вместимость</param>
/// <param name="PaidCount">Сколько нерасселённых заявок оплачено полностью</param>
public record RoomTypeOccupancySummary(
    int Capacity,
    int Occupied,
    int RoomsCount,
    int ApprovedClaims,
    int FullyFreeRoomsCount,
    int FullyOccupiedRoomsCount,
    int PaidCount)
{
    /// <summary>
    /// Всего мест этого типа
    /// </summary>
    public int TotalCapacity => RoomsCount * Capacity;

    /// <summary>
    /// Сколько мест этого типа свободно
    /// </summary>
    public int FreeCapacity => TotalCapacity - Occupied;

    /// <summary>
    /// Заявки, которые выбрали этот тип, но ещё не расселены по комнатам
    /// </summary>
    public int PendingRequests => ApprovedClaims - Occupied;

    /// <summary>
    /// Комнаты, занятые частично: ни пустые, ни занятые под вместимость
    /// </summary>
    public int PartialRoomsCount => RoomsCount - FullyFreeRoomsCount - FullyOccupiedRoomsCount;

    /// <summary>
    /// Свободные места в частично занятых комнатах
    /// </summary>
    public int PartialFreeSeats => FreeCapacity - (Capacity * FullyFreeRoomsCount);

    /// <summary>
    /// Занятые места в частично занятых комнатах
    /// </summary>
    public int PartialOccupiedSeats => Occupied - (Capacity * FullyOccupiedRoomsCount);

    /// <summary>
    /// Нерасселённые заявки, по которым взнос оплачен не полностью
    /// </summary>
    public int AcceptedNotPaidCount => PendingRequests - PaidCount;
}

/// <summary>
/// Строка таблицы типов проживания на странице «Поселение».
/// </summary>
/// <remarks>
/// Настройки типа берутся из метаданных проекта (ADR015), занятость — из оперативной сводки
/// <see cref="Occupancy"/>.
/// </remarks>
public class RoomTypeListItemViewModel : RoomTypeViewModelBase
{
    /// <summary>
    /// Оперативная занятость этого типа проживания: счётчики и арифметика формул строки.
    /// </summary>
    public RoomTypeOccupancySummary Occupancy { get; }

    public override int RoomsCount => Occupancy.RoomsCount;

    /// <param name="typeInfo">Настройки типа проживания из метаданных проекта (ADR015)</param>
    /// <param name="descriptionView">
    /// Описание типа проживания, уже отрендеренное из Markdown вызывающей стороной
    /// (рендерер markdown живёт на сервере, см. <see cref="RoomTypeViewModelBase.DescriptionView"/>).
    /// </param>
    /// <param name="occupancy">
    /// Оперативная занятость этого типа проживания. Вместимость в ней — та же, что в
    /// <paramref name="typeInfo"/>: сводка складывается из счётчиков SQL и настройки типа.
    /// </param>
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

        Occupancy = occupancy;
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
