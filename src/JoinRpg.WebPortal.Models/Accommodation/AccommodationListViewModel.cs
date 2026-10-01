using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;

namespace JoinRpg.Web.Models.Accommodation;

public class AccommodationListViewModel
{
    public int ProjectId { get; set; }

    [DisplayName("Проект")]
    public string ProjectName { get; set; }

    [DisplayName("Типы проживания")]
    public List<RoomTypeListItemViewModel> RoomTypes { get; set; }

    public bool CanAssignRooms { get; set; }
    public bool CanManageRooms { get; set; }

    public int? TotalCapacity { get; set; }

    public int? FreeCapacity { get; set; }

    public int TotalOccupied { get; set; }

    public int TotalPending { get; set; }

    public bool IsInfinite { get; set; }

    public int TotalPaid { get; }

    public int TotalAcceptedNotPaid { get; }

    public UnassignedClaimsRowViewModel UnassignedClaims { get; set; }

    /// <param name="roomTypes">Оперативная сводка занятости по типам проживания</param>
    /// <param name="claimsWithoutRoomType">Активные заявки, в которых тип проживания не выбран</param>
    /// <param name="unsettledClaims">
    /// Заявки, выбравшие тип проживания, но ещё не расселённые по комнатам: по ним считается, сколько
    /// нерасселённых оплачено. Баланс считается здесь, а не в строке таблицы: расчёт идёт по
    /// EF-графу заявки, а <see cref="RoomTypeListItemViewModel"/> на EF не смотрит.
    /// </param>
    public AccommodationListViewModel(ProjectInfo project,
        IReadOnlyCollection<RoomTypeInfoRow> roomTypes,
        IReadOnlyCollection<Claim> claimsWithoutRoomType,
        IReadOnlyCollection<Claim> unsettledClaims,
        ICurrentUserAccessor userId)
    {
        ProjectId = project.ProjectId;
        ProjectName = project.ProjectName;
        CanManageRooms = project.HasMasterAccess(userId, Permission.CanManageAccommodation);
        CanAssignRooms = project.HasMasterAccess(userId, Permission.CanSetPlayersAccommodations);

        var paidByRoomType = unsettledClaims
            .GroupBy(claim => claim.AccommodationRequest!.AccommodationTypeId)
            .ToDictionary(
                group => group.Key,
                group => AccommodationClaimCounters.CountPaid(group, project));

        RoomTypes = [.. roomTypes.Select(row =>
        {
            // Тип проживания — настройка проекта, он уже есть в метаданных (ADR015).
            var typeInfo = project.AccommodationSettings.GetTypeById(row.RoomTypeId);
            return new RoomTypeListItemViewModel(
                typeInfo,
                // Markdown рендерится здесь, на сервере: вью-модель лежит в браузерной библиотеке
                // JoinRpg.Web.Accommodation и рендерера markdown не видит.
                typeInfo.Description.ToHtmlString(),
                new RoomTypeOccupancySummary(
                    row.Occupied,
                    row.RoomsCount,
                    row.ApprovedClaims,
                    row.FullyFreeRoomsCount,
                    row.FullyOccupiedRoomsCount,
                    paidByRoomType.GetValueOrDefault(row.RoomTypeId.AccommodationTypeId)),
                userId.UserIdentification,
                project);
        })];

        IsInfinite = RoomTypes.Any(rt => rt.IsInfinite);

        var allInfinite = RoomTypes.All(rt => rt.IsInfinite);
        TotalCapacity = allInfinite ? (int?)null : RoomTypes.Sum(rt => rt.TotalCapacity);
        FreeCapacity = allInfinite ? (int?)null : RoomTypes.Sum(rt => rt.FreeCapacity);

        TotalOccupied = RoomTypes.Sum(x => x.Occupied);

        UnassignedClaims = new UnassignedClaimsRowViewModel(claimsWithoutRoomType, project)
        {
            ProjectId = project.ProjectId,
        };

        TotalPending = RoomTypes.Sum(x => x.PendingRequests) + UnassignedClaims.PendingRequests;

        TotalPaid = RoomTypes.Sum(rt => rt.PaidCount) + UnassignedClaims.PaidCount;
        TotalAcceptedNotPaid = RoomTypes.Sum(rt => rt.AcceptedNotPaidCount) + UnassignedClaims.AcceptedNotPaidCount;
    }
}

/// <summary>
/// Виртуальная строка для игроков, у которых не выбран тип поселения
/// </summary>
public class UnassignedClaimsRowViewModel
{
    public int ProjectId { get; set; }

    public int PendingRequests { get; }

    public int PaidCount { get; }

    public int AcceptedNotPaidCount { get; }

    public UnassignedClaimsRowViewModel(IReadOnlyCollection<Claim> claimsWithoutRoomType, ProjectInfo projectInfo)
    {
        PendingRequests = claimsWithoutRoomType.Count;
        PaidCount = AccommodationClaimCounters.CountPaid(claimsWithoutRoomType, projectInfo);
        AcceptedNotPaidCount = PendingRequests - PaidCount;
    }
}

/// <summary>
/// Подсчёт оплаченных заявок для страницы «Поселение».
/// </summary>
/// <remarks>
/// Баланс считается по EF-графу заявки (<c>FinanceExtensions.CalculateClaimBalance</c>, помечен
/// <c>[Obsolete]</c>). Замена — расчёт поверх агрегата персонажа (ADR013), как это уже сделано на
/// странице комнат; до тех пор страница «Поселение» держит EF-сущности.
/// </remarks>
internal static class AccommodationClaimCounters
{
    public static int CountPaid(IEnumerable<Claim> claims, ProjectInfo projectInfo)
        => claims.Count(claim =>
        {
            var balance = claim.CalculateClaimBalance(projectInfo);
            var status = FinanceExtensions.GetClaimPaymentStatus(balance.TotalFee, balance.FeePaid);
            return status is ClaimPaymentStatus.Paid or ClaimPaymentStatus.Overpaid;
        });
}
