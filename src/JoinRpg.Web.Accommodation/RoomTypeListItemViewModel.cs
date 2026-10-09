using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Models.Accommodation;

/// <summary>
/// Оперативная занятость одного типа проживания для страницы «Поселение»: счётчики и вся
/// арифметика формул, которые по ним показывает разметка.
/// </summary>
/// <remarks>
/// В метаданные проекта это не входит: занятость считается по плану категории комнат
/// (<see cref="FromPlan"/>, ADR020), а <paramref name="PaidCount"/> — расчёт баланса по
/// нерасселённым заявкам. Отдельный тип, а не ряд репозитория, потому что
/// вью-модель лежит в браузерной библиотеке и на DAL не смотрит.
/// </remarks>
/// <param name="Capacity">
/// Вместимость одной комнаты пула — <see cref="RoomCategoryPlan.RoomCapacity"/>. Нужна здесь, чтобы
/// раскладывать места по полностью и частично занятым комнатам.
/// </param>
/// <param name="Occupied">Сколько мест занято жильцами именно этого типа</param>
/// <param name="PoolOccupied">
/// Сколько мест занято в комнатах пула — всеми его типами (ADR020). Комнатные счётчики строки
/// описывают пул, а не тип: комнаты у типов одной категории общие.
/// </param>
/// <param name="RoomsCount">Сколько комнат в пуле</param>
/// <param name="ApprovedClaims">Сколько заявок выбрало этот тип проживания — и расселённых, и нет</param>
/// <param name="FullyFreeRoomsCount">Комнаты пула, в которых не живёт никто</param>
/// <param name="FullyOccupiedRoomsCount">Комнаты пула, занятые под вместимость</param>
/// <param name="FullyOccupiedSeats">
/// Сколько человек живёт в полностью занятых комнатах. Не «вместимость × комнаты»: комнату
/// заполняет и тип с меньшей вместимостью («Люкс на одного» в двухместном «Люксе»).
/// </param>
/// <param name="FreeCapacity">
/// Сколько мест в пуле свободно — по эффективной вместимости комнат
/// (<see cref="RoomCategoryPlan.FreeCapacity"/>).
/// </param>
/// <param name="PaidCount">Сколько нерасселённых заявок оплачено полностью</param>
public record RoomTypeOccupancySummary(
    int Capacity,
    int Occupied,
    int PoolOccupied,
    int RoomsCount,
    int ApprovedClaims,
    int FullyFreeRoomsCount,
    int FullyOccupiedRoomsCount,
    int FullyOccupiedSeats,
    int FreeCapacity,
    int PaidCount)
{
    /// <summary>
    /// Сводка типа по плану его категории комнат.
    /// </summary>
    public static RoomTypeOccupancySummary FromPlan(
        RoomCategoryPlan plan,
        AccommodationTypeIdentification typeId,
        int paidCount)
    {
        var groupsOfType = plan.Groups.Where(group => group.AccommodationTypeId == typeId).ToList();
        var fullRooms = plan.Rooms.Where(room => room.IsOccupied && plan.IsFull(room.Id)).ToList();
        return new(
            Capacity: plan.RoomCapacity,
            Occupied: groupsOfType.Where(group => group.RoomId is not null).Sum(group => group.SubjectsCount),
            PoolOccupied: plan.Occupancy,
            RoomsCount: plan.Rooms.Count,
            ApprovedClaims: groupsOfType.Sum(group => group.SubjectsCount),
            FullyFreeRoomsCount: plan.Rooms.Count(room => !room.IsOccupied),
            FullyOccupiedRoomsCount: fullRooms.Count,
            FullyOccupiedSeats: fullRooms.Sum(room => room.Occupancy),
            FreeCapacity: plan.FreeCapacity,
            PaidCount: paidCount);
    }

    /// <summary>
    /// Всего мест в пуле
    /// </summary>
    public int TotalCapacity => RoomsCount * Capacity;

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
    public int PartialOccupiedSeats => PoolOccupied - FullyOccupiedSeats;

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

    public int RoomsCount => Occupancy.RoomsCount;

    [DisplayName("Общее количество мест")]
    public int TotalCapacity => RoomsCount * Capacity;

    /// <param name="typeInfo">Настройки типа проживания из метаданных проекта (ADR015)</param>
    /// <param name="descriptionView">
    /// Описание типа проживания, уже отрендеренное из Markdown вызывающей стороной
    /// (рендерер markdown живёт на сервере, см. <see cref="RoomTypeViewModelBase.DescriptionView"/>).
    /// </param>
    /// <param name="occupancy">
    /// Оперативная занятость этого типа проживания и его пула. Вместимость в ней — вместимость
    /// комнаты пула, а не <paramref name="typeInfo"/>: у типов одной категории она общая (ADR020).
    /// </param>
    /// <param name="currentUserId">
    /// Пользователь, который смотрит страницу: по нему считаются права
    /// <see cref="RoomTypeViewModelBase.CanManageRooms"/> и
    /// <see cref="RoomTypeViewModelBase.CanAssignRooms"/>.
    /// </param>
    /// <remarks>
    /// <c>IsInfinite</c> и <c>IsAutoFilledAccommodation</c> берутся из метаданных явно, хотя там
    /// всегда <c>false</c>: функциональность не реализована (см.
    /// <see cref="AccommodationTypeInfo.IsInfinite"/>), и строка должна показывать именно это
    /// значение, а не совпадающий с ним по случайности дефолт вью-модели.
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
        IsInfinite = typeInfo.IsInfinite;
        IsAutoFilledAccommodation = typeInfo.IsAutoFilledAccommodation;
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
    public const string FullyOccupiedSeatsTooltip = "количество занятых мест в полностью занятых номерах";
    public const string PartialOccupiedSeatsTooltip = "количество занятых мест в частично занятых номерах";
    public const string TotalOccupiedTooltip = "всего занятых мест в этой категории";
    public const string PaidTooltip = "оплаченных";
    public const string AcceptedNotPaidTooltip = "принятых, не оплаченных";
    public const string TotalUnsettledTooltip = "всего";
}
