using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Data.Interfaces.Claims;

public interface IClaimsRepository : IDisposable
{
    Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status);

    Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(UserIdentification userId, ClaimStatusSpec status);

    /// <summary>
    /// Активные заявки пользователя в активных проектах, без лишних данных (для меню и подобных списков)
    /// </summary>
    Task<IReadOnlyCollection<MyClaimShortInfo>> GetMyActiveClaimsInActiveProjects(UserIdentification userId);

    Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(ProjectIdentification projectId, UserIdentification userId, ClaimStatusSpec status);

    Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<ClaimIdentification> claimIds);
    Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status);

    Task<Claim?> GetClaim(ClaimIdentification claimId);
    Task<Claim?> GetClaimWithDetails(ClaimIdentification claimId);

    Task<IReadOnlyCollection<Claim>> GetClaimsForGroups(ProjectIdentification projectId, ClaimStatusSpec active, CharacterGroupIdentification[] characterGroupsIds);

    Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<CharacterGroupIdentification> characterGroupsIds, ClaimStatusSpec spec);
    Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId);

    Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimsHeadersForPlayer(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec, UserIdentification userId);

    Task<IReadOnlyCollection<ClaimCountByMaster>> GetClaimsCountByMasters(int projectId, ClaimStatusSpec claimStatusSpec);

    Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(ProjectIdentification projectId, ClaimStatusSpec approved);
    Task<IReadOnlyCollection<Claim>> GetClaimsForRoomType(int projectId, ClaimStatusSpec claimStatusSpec, int? roomTypeId);

    /// <summary>
    /// Заявки проекта, у которых тип проживания выбран, а комната ещё не назначена
    /// («нерасселённые»), вместе с данными для расчёта баланса.
    /// </summary>
    /// <remarks>
    /// Фильтра по статусу нет намеренно — ровно так же считала страница «Поселение», когда обходила
    /// <c>ProjectAccommodationType.Desirous</c>: отклонённая заявка выбывает из группы проживания
    /// сама (<c>ClaimServiceImpl.ConsiderLeavingRoom</c>), так что фильтр ничего бы не изменил, а
    /// заявке «на удержании» место в номере по-прежнему числится.
    /// </remarks>
    Task<IReadOnlyCollection<Claim>> GetUnsettledAccommodationClaims(ProjectIdentification projectId);

    /// <summary>
    /// Заявки проекта ровно в статусе <see cref="ClaimStatus.Approved"/>, не состоящие ни в одной
    /// группе проживающих, — кандидаты «ещё не выбрал тип проживания» для виджета приглашений.
    /// </summary>
    /// <remarks>
    /// Плана поселения для них нет: без типа заявка не попадает ни в один план (ADR022 §3).
    /// </remarks>
    Task<IReadOnlyCollection<ClaimWithPlayer>> GetApprovedClaimHeadersWithoutAccommodation(ProjectIdentification projectId);

    Task<Dictionary<int, int>> GetUnreadDiscussionsForClaims(int projectId, ClaimStatusSpec claimStatusSpec, int userId, bool hasMasterAccess);

}
