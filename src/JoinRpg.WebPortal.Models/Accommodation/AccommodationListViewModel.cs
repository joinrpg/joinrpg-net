using JoinRpg.Data.Interfaces;
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

    public string TotalUnsettledTooltip { get; }

    public AccommodationListViewModel(ProjectInfo project,
        IReadOnlyCollection<RoomTypeInfoRow> roomTypes,
        ICurrentUserAccessor userId)
    {
        ProjectId = project.ProjectId;
        ProjectName = project.ProjectName;
        CanManageRooms = project.HasMasterAccess(userId, Permission.CanManageAccommodation);
        CanAssignRooms = project.HasMasterAccess(userId, Permission.CanSetPlayersAccommodations);
        RoomTypes = roomTypes.Select(rt => new RoomTypeListItemViewModel(rt, userId, project)).ToList();

        IsInfinite = RoomTypes.Any(rt => rt.IsInfinite);

        var allInfinite = RoomTypes.All(rt => rt.IsInfinite);
        TotalCapacity = allInfinite ? (int?)null : RoomTypes.Sum(rt => rt.TotalCapacity);
        FreeCapacity = allInfinite ? (int?)null : RoomTypes.Sum(rt => rt.FreeCapacity);

        TotalOccupied = RoomTypes.Sum(x => x.Occupied);
        TotalPending = RoomTypes.Sum(x => x.PendingRequests);

        TotalUnsettledTooltip = RoomTypeListItemViewModel.BuildUnsettledTooltip(
            RoomTypes.Sum(rt => rt.PaidCount),
            RoomTypes.Sum(rt => rt.AcceptedNotPaidCount));
    }
}

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

    public RoomTypeListItemViewModel(RoomTypeInfoRow row, ICurrentUserAccessor userId, ProjectInfo projectInfo)
    {
        var entity = row.RoomType;
        var project = row.RoomType.Project;
        if (entity.ProjectId == 0 || entity.Id == 0)
        {
            throw new ArgumentException("Entity must be valid object");
        }
        Id = entity.Id;
        Cost = entity.Cost;
        Name = entity.Name;
        Capacity = entity.Capacity;
        IsInfinite = entity.IsInfinite;

        IsPlayerSelectable = entity.IsPlayerSelectable;
        IsAutoFilledAccommodation = entity.IsAutoFilledAccommodation;
        DescriptionView = ((MarkdownString?)entity.Description).ToHtmlString();
        ProjectId = project.ProjectId;
        CanManageRooms = project.HasMasterAccess(userId, Permission.CanManageAccommodation);
        CanAssignRooms = project.HasMasterAccess(userId, Permission.CanSetPlayersAccommodations);

        Occupied = row.Occupied;
        RoomsCount = row.RoomsCount;
        ApprovedClaims = row.ApprovedClaims;

        FreeCapacity = TotalCapacity - Occupied;
        PendingRequests = ApprovedClaims - Occupied;

        FullyFreeRoomsCount = row.FullyFreeRoomsCount;
        FullyOccupiedRoomsCount = row.FullyOccupiedRoomsCount;
        PartialRoomsCount = RoomsCount - FullyFreeRoomsCount - FullyOccupiedRoomsCount;
        PartialFreeSeats = FreeCapacity - (Capacity * FullyFreeRoomsCount);
        PartialOccupiedSeats = Occupied - (Capacity * FullyOccupiedRoomsCount);

        var unsettledClaims = entity.Desirous
            .Where(ar => ar.AccommodationId == null)
            .SelectMany(ar => ar.Subjects);

        PaidCount = unsettledClaims.Count(claim =>
        {
            var balance = claim.CalculateClaimBalance(projectInfo);
            var status = FinanceExtensions.GetClaimPaymentStatus(balance.TotalFee, balance.FeePaid);
            return status is ClaimPaymentStatus.Paid or ClaimPaymentStatus.Overpaid;
        });
        AcceptedNotPaidCount = PendingRequests - PaidCount;
    }

    public int ApprovedClaims { get; set; }
    public int FreeCapacity { get; }

    public string FreeTooltip
        => PartialRoomsCount > 0
            ? $"{Capacity} (это вместимость номера) х {FullyFreeRoomsCount} (это кол-во полностью свободных номеров) + {PartialFreeSeats} (количество свободных мест в частично занятых номерах) = {FreeCapacity} (всего свободных мест в этой категории)"
            : $"{Capacity} (это вместимость номера) х {FullyFreeRoomsCount} (это кол-во полностью свободных номеров) = {FreeCapacity} (всего свободных мест в этой категории)";

    public string OccupiedTooltip
        => PartialRoomsCount > 0
            ? $"{Capacity} (это вместимость номера) х {FullyOccupiedRoomsCount} (это кол-во полностью занятых номеров) + {PartialOccupiedSeats} (количество занятых мест в частично занятых номерах) = {Occupied} (всего занятых мест в этой категории)"
            : $"{Capacity} (это вместимость номера) х {FullyOccupiedRoomsCount} (это кол-во полностью занятых номеров) = {Occupied} (всего занятых мест в этой категории)";

    public string UnsettledTooltip => BuildUnsettledTooltip(PaidCount, AcceptedNotPaidCount);

    public static string BuildUnsettledTooltip(int paidCount, int acceptedNotPaidCount)
    {
        var parts = new List<string>();
        if (paidCount != 0 || acceptedNotPaidCount == 0)
        {
            parts.Add($"{paidCount} (оплаченных)");
        }
        if (acceptedNotPaidCount != 0)
        {
            parts.Add($"{acceptedNotPaidCount} (принятых, не оплаченных)");
        }
        return $"{string.Join(" + ", parts)} = {paidCount + acceptedNotPaidCount} (всего)";
    }
}
