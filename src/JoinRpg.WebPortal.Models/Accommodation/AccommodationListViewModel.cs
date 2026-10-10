using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
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

    /// <param name="plans">
    /// Планы всех категорий комнат проекта (ADR018): из них считаются и комнатные счётчики пула,
    /// и занятость каждого типа. Комнаты у типов одной категории общие (ADR020), поэтому итоги
    /// страницы складываются по категориям, а не по типам.
    /// </param>
    /// <param name="claimsWithoutRoomType">Активные заявки, в которых тип проживания не выбран</param>
    /// <param name="unsettledCharacters">
    /// Снимки персонажей (ADR013), на которых поданы заявки из нерасселённых групп планов: по ним
    /// считается, сколько нерасселённых оплачено. Какие заявки нерасселённые и какого они типа —
    /// решает план (ADR022), а не навигация группы; лишние заявки этих персонажей не считаются.
    /// </param>
    public AccommodationListViewModel(ProjectInfo project,
        IReadOnlyCollection<RoomCategoryPlan> plans,
        IReadOnlyCollection<Claim> claimsWithoutRoomType,
        IReadOnlyCollection<CharacterInfo> unsettledCharacters,
        ICurrentUserAccessor userId)
    {
        ProjectId = project.ProjectId;
        ProjectName = project.ProjectName;
        CanManageRooms = project.HasMasterAccess(userId, Permission.CanManageAccommodation);
        CanAssignRooms = project.HasMasterAccess(userId, Permission.CanSetPlayersAccommodations);

        var paidByRoomType = AccommodationClaimCounters.CountUnsettledPaid(plans, unsettledCharacters);

        var planByCategory = plans.ToDictionary(plan => plan.Id);

        // Тип проживания — настройка проекта, он уже есть в метаданных (ADR015).
        RoomTypes = [.. project.AccommodationSettings.Types.Select(typeInfo => new RoomTypeListItemViewModel(
            typeInfo,
            // Markdown рендерится здесь, на сервере: вью-модель лежит в браузерной библиотеке
            // JoinRpg.Web.Accommodation и рендерера markdown не видит.
            typeInfo.Description.ToHtmlString(),
            RoomTypeOccupancySummary.FromPlan(
                planByCategory[typeInfo.RoomCategoryId],
                typeInfo.Id,
                paidByRoomType.GetValueOrDefault(typeInfo.Id)),
            userId.UserIdentification,
            project))];

        IsInfinite = RoomTypes.Any(rt => rt.IsInfinite);

        var allInfinite = RoomTypes.All(rt => rt.IsInfinite);
        TotalCapacity = allInfinite ? (int?)null : plans.Sum(plan => plan.TotalCapacity);
        FreeCapacity = allInfinite ? (int?)null : plans.Sum(plan => plan.FreeCapacity);

        TotalOccupied = RoomTypes.Sum(x => x.Occupancy.Occupied);

        UnassignedClaims = new UnassignedClaimsRowViewModel(claimsWithoutRoomType, project)
        {
            ProjectId = project.ProjectId,
        };

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
/// Нерасселённые считаются по доменным снимкам (ADR013, ADR022), как и на странице комнат.
/// Строка заявок без типа проживания пока считает по EF-графу заявки
/// (<c>FinanceExtensions.CalculateClaimBalance</c>, помечен <c>[Obsolete]</c>).
/// </remarks>
internal static class AccommodationClaimCounters
{
    public static int CountPaid(IEnumerable<Claim> claims, ProjectInfo projectInfo)
        => claims.Count(claim => claim.CalculateClaimBalance(projectInfo).IsPaid);

    /// <summary>
    /// Сколько заявок нерасселённых групп оплачено полностью — по типам проживания групп.
    /// </summary>
    /// <remarks>
    /// Состав и тип группы берутся из плана, взнос (с проживанием — <c>ClaimFinanceInfo.AccommodationFee</c>)
    /// — из снимка заявки. Заявка, снимка которой нет среди <paramref name="characters"/>, не считается.
    /// </remarks>
    public static IReadOnlyDictionary<AccommodationTypeIdentification, int> CountUnsettledPaid(
        IEnumerable<RoomCategoryPlan> plans,
        IEnumerable<CharacterInfo> characters)
    {
        var claims = characters
            .SelectMany(character => character.Claims.Select(claim => new ClaimInCharacter(character, claim)))
            .ToDictionary(claim => claim.ClaimId);

        return plans
            .SelectMany(plan => plan.UnassignedGroups)
            .GroupBy(group => group.AccommodationTypeId)
            .ToDictionary(
                byType => byType.Key,
                byType => byType
                    .SelectMany(group => group.Subjects)
                    .Count(claimId => claims.TryGetValue(claimId, out var claim) && claim.CalculateBalance().IsPaid));
    }
}
