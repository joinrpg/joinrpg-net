using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;

namespace JoinRpg.Web.Models.Accommodation;

/// <summary>
/// Собирает <see cref="AccommodationListViewModel"/> для страницы «Поселение». Сама вью-модель
/// лежит в браузерной библиотеке <c>JoinRpg.Web.Accommodation</c>, а здесь — то, что ей недоступно:
/// рендер описаний из Markdown и подсчёт оплаченных заявок по снимкам персонажей.
/// </summary>
public static class AccommodationListViewModelBuilder
{
    /// <param name="plans">
    /// Планы всех категорий комнат проекта (ADR018): из них считаются и комнатные счётчики пула,
    /// и занятость каждого типа. Комнаты у типов одной категории общие (ADR020), поэтому итоги
    /// страницы складываются по категориям, а не по типам.
    /// </param>
    /// <param name="claimIdsWithoutRoomType">Активные заявки, в которых тип проживания не выбран</param>
    /// <param name="characters">
    /// Снимки персонажей (ADR013), на которых поданы заявки из нерасселённых групп планов и заявки
    /// без типа проживания: по ним считается, сколько из них оплачено. Какие заявки нерасселённые и
    /// какого они типа — решает план (ADR022), а не навигация группы; лишние заявки этих персонажей
    /// не считаются.
    /// </param>
    public static AccommodationListViewModel Build(ProjectInfo project,
        IReadOnlyCollection<RoomCategoryPlan> plans,
        IReadOnlyCollection<ClaimIdentification> claimIdsWithoutRoomType,
        IReadOnlyCollection<CharacterInfo> characters,
        ICurrentUserAccessor userId)
    {
        var counters = new AccommodationClaimCounters(characters);
        var paidByRoomType = counters.CountUnsettledPaid(plans);

        var planByCategory = plans.ToDictionary(plan => plan.Id);

        // Тип проживания — настройка проекта, он уже есть в метаданных (ADR015).
        List<RoomTypeListItemViewModel> roomTypes = [.. project.AccommodationSettings.Types.Select(typeInfo => new RoomTypeListItemViewModel(
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

        var unassignedClaims = new UnassignedClaimsRowViewModel(
            project.ProjectId,
            claimIdsWithoutRoomType.Count,
            counters.CountPaid(claimIdsWithoutRoomType));

        return new AccommodationListViewModel(project, plans, roomTypes, unassignedClaims, userId.UserIdentification);
    }
}

/// <summary>
/// Подсчёт оплаченных заявок для страницы «Поселение» — по доменным снимкам заявок (ADR013, ADR022),
/// как и на странице комнат.
/// </summary>
/// <remarks>
/// Взнос (с проживанием — <c>ClaimFinanceInfo.AccommodationFee</c>) берётся из снимка заявки.
/// Заявка, снимка которой нет среди переданных персонажей, оплаченной не считается.
/// </remarks>
internal sealed class AccommodationClaimCounters(IEnumerable<CharacterInfo> characters)
{
    private readonly Dictionary<ClaimIdentification, ClaimInCharacter> claims = characters
        .SelectMany(character => character.Claims.Select(claim => new ClaimInCharacter(character, claim)))
        .ToDictionary(claim => claim.ClaimId);

    /// <summary>
    /// Сколько из указанных заявок оплачено полностью.
    /// </summary>
    public int CountPaid(IEnumerable<ClaimIdentification> claimIds)
        => claimIds.Count(claimId => claims.TryGetValue(claimId, out var claim) && claim.CalculateBalance().IsPaid);

    /// <summary>
    /// Сколько заявок нерасселённых групп оплачено полностью — по типам проживания групп.
    /// Состав и тип группы берутся из плана.
    /// </summary>
    public IReadOnlyDictionary<AccommodationTypeIdentification, int> CountUnsettledPaid(IEnumerable<RoomCategoryPlan> plans)
        => plans
            .SelectMany(plan => plan.UnassignedGroups)
            .GroupBy(group => group.AccommodationTypeId)
            .ToDictionary(
                byType => byType.Key,
                byType => CountPaid(byType.SelectMany(group => group.Subjects)));
}
