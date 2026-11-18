using JoinRpg.DomainTypes.Accommodation;

namespace JoinRpg.Web.Models.Accommodation;

/// <summary>
/// Страница «Поселение» (<c>{projectId}/rooms/</c>): таблица типов проживания и итоги по ним.
/// </summary>
/// <remarks>
/// Строки собирает серверная сторона: описание типа рендерится из Markdown, а оплаченность считается
/// по снимкам персонажей (ADR013, ADR022) — ни рендерера, ни расчёта баланса эта библиотека не видит.
/// Здесь же — только итоги поверх готовых строк.
/// </remarks>
public class AccommodationListViewModel
{
    public ProjectIdentification ProjectId { get; }

    [DisplayName("Проект")]
    public string ProjectName { get; }

    [DisplayName("Типы проживания")]
    public IReadOnlyList<RoomTypeListItemViewModel> RoomTypes { get; }

    public bool CanAssignRooms { get; }
    public bool CanManageRooms { get; }

    public int? FreeCapacity { get; }

    public int TotalOccupied { get; }

    public int TotalPending { get; }

    public int TotalPaid { get; }

    public int TotalAcceptedNotPaid { get; }

    public UnassignedClaimsRowViewModel UnassignedClaims { get; }

    /// <param name="plans">
    /// Планы всех категорий комнат проекта (ADR018). Комнаты у типов одной категории общие (ADR020),
    /// поэтому свободные места страницы складываются по категориям, а не по типам.
    /// </param>
    /// <param name="roomTypes">Строки типов проживания</param>
    /// <param name="unassignedClaims">Строка заявок, в которых тип проживания не выбран</param>
    /// <param name="currentUserId">Пользователь, который смотрит страницу: по нему считаются права</param>
    public AccommodationListViewModel(
        ProjectInfo project,
        IReadOnlyCollection<RoomCategoryPlan> plans,
        IReadOnlyList<RoomTypeListItemViewModel> roomTypes,
        UnassignedClaimsRowViewModel unassignedClaims,
        UserIdentification currentUserId)
    {
        ProjectId = project.ProjectId;
        ProjectName = project.ProjectName;
        CanManageRooms = project.HasMasterAccess(currentUserId, Permission.CanManageAccommodation);
        CanAssignRooms = project.HasMasterAccess(currentUserId, Permission.CanSetPlayersAccommodations);

        RoomTypes = roomTypes;
        UnassignedClaims = unassignedClaims;

        FreeCapacity = RoomTypes.All(rt => rt.IsInfinite) ? null : plans.Sum(plan => plan.FreeCapacity);

        TotalOccupied = RoomTypes.Sum(x => x.Occupancy.Occupied);

        TotalPending = RoomTypes.Sum(x => x.Occupancy.PendingRequests) + UnassignedClaims.PendingRequests;

        TotalPaid = RoomTypes.Sum(rt => rt.Occupancy.PaidCount) + UnassignedClaims.PaidCount;
        TotalAcceptedNotPaid = RoomTypes.Sum(rt => rt.Occupancy.AcceptedNotPaidCount) + UnassignedClaims.AcceptedNotPaidCount;
    }
}

/// <summary>
/// Виртуальная строка для игроков, у которых не выбран тип поселения
/// </summary>
public class UnassignedClaimsRowViewModel
{
    public ProjectIdentification ProjectId { get; }

    public int PendingRequests { get; }

    public int PaidCount { get; }

    public int AcceptedNotPaidCount { get; }

    public UnassignedClaimsRowViewModel(ProjectIdentification projectId, int pendingRequests, int paidCount)
    {
        ProjectId = projectId;
        PendingRequests = pendingRequests;
        PaidCount = paidCount;
        AcceptedNotPaidCount = PendingRequests - PaidCount;
    }
}
