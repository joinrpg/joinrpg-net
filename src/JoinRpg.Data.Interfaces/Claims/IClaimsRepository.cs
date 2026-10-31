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
    /// Заявки проекта ровно в статусе <see cref="ClaimStatus.Approved"/>, не состоящие ни в одной
    /// группе проживающих, — кандидаты «ещё не выбрал тип проживания» для виджета приглашений.
    /// </summary>
    /// <remarks>
    /// Плана поселения для них нет: без типа заявка не попадает ни в один план (ADR022 §3).
    /// </remarks>
    Task<IReadOnlyCollection<ClaimWithPlayer>> GetApprovedClaimHeadersWithoutAccommodation(ProjectIdentification projectId);

    /// <summary>
    /// Идентификаторы активных заявок проекта (<see cref="ClaimStatusSpec.Active"/>), не состоящих
    /// ни в одной группе проживающих, — строка «без типа проживания» сводки «Поселение».
    /// </summary>
    /// <remarks>
    /// Только идентификаторы: баланс считается по снимкам персонажей (ADR013), которые вызывающий
    /// грузит одной выборкой <c>ICharacterInfoRepository.GetCharacterInfosByClaims</c>.
    /// </remarks>
    Task<IReadOnlyCollection<ClaimIdentification>> GetActiveClaimIdsWithoutAccommodation(ProjectIdentification projectId);

    /// <summary>
    /// Число непрочитанных пользователем комментариев в обсуждении каждой заявки проекта.
    /// </summary>
    Task<Dictionary<CommentDiscussionId, int>> GetUnreadDiscussionsForClaims(int projectId, ClaimStatusSpec claimStatusSpec, int userId, bool hasMasterAccess);

    /// <summary>
    /// Номера заявок перечисленных игроков в одном проекте — во всех статусах и без агрегатов.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Только номера: полный агрегат заявки догружает персонажа, операции и игрока, но не проект,
    /// а сверке списка платежей с базой (ADR028) нужен один номер.
    /// </para>
    /// <para>
    /// Отклонённые и снятые с обсуждения заявки отдаются тоже: потерянный платёж бывает и на
    /// закрытой заявке. Выбирает из них вызывающий.
    /// </para>
    /// </remarks>
    Task<IReadOnlyCollection<PlayerClaimId>> GetClaimIdsForPlayers(ProjectIdentification projectId, IReadOnlyCollection<UserIdentification> userIds);

    /// <summary>
    /// Заявки, в чьём обсуждении лежат перечисленные комментарии, — одним запросом на весь набор.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Отдаются только комментарии, принадлежащие обсуждению заявки: комментарии форумных тем
    /// отсутствуют по построению — ни одна заявка не ссылается на их обсуждение.
    /// </para>
    /// <para>
    /// Назначение (ADR028): комментарий, возвращённый инструментами DRP без своей финансовой
    /// операции, называет заявку достовернее, чем эвристика «наименьший номер среди заявок
    /// плательщика в проекте».
    /// </para>
    /// </remarks>
    Task<IReadOnlyCollection<CommentClaimId>> GetClaimIdsByCommentIds(IReadOnlyCollection<int> commentIds);

    /// <summary>
    /// Число непрочитанных смотрящим комментариев в обсуждении каждой заявки игрока в любом статусе
    /// во всех проектах («Мои заявки» на профиле).
    /// </summary>
    /// <remarks>
    /// Скрытые от игроков комментарии считаются в тех проектах, где смотрящий — действующий мастер.
    /// </remarks>
    Task<Dictionary<CommentDiscussionId, int>> GetUnreadDiscussionsForPlayerClaims(UserIdentification playerId, UserIdentification viewerId);

}

/// <summary>
/// Заявка игрока в проекте: кто и какая заявка.
/// </summary>
/// <param name="UserId">Игрок (<c>Claim.PlayerUserId</c>).</param>
/// <param name="ClaimId">Идентификатор заявки.</param>
public record PlayerClaimId(UserIdentification UserId, ClaimIdentification ClaimId);

/// <summary>
/// Комментарий и заявка, в чьём обсуждении он лежит.
/// </summary>
/// <param name="CommentId">Идентификатор комментария.</param>
/// <param name="ClaimId">Идентификатор заявки — владельца обсуждения комментария.</param>
public record CommentClaimId(int CommentId, ClaimIdentification ClaimId);
