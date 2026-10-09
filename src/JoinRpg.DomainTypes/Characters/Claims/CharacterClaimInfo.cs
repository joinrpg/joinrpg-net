using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Finances;

namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Базовые сведения о заявке на персонажа — часть агрегата <see cref="CharacterInfo"/> (ADR013).
/// Несёт ровно то, что нужно гридам, подсчёту проблем и расчёту баланса; полных данных о
/// комментариях (сами <c>Comment</c> / <c>CommentDiscussion</c>) и о сюжетах здесь нет.
/// </summary>
/// <param name="Player">
/// Игрок, подавший заявку, вместе с отображаемым именем. Имя входит в агрегат, потому что по нему
/// именуется персонаж, когда в проекте не настроено поле-имя; остальной профиль (контакты, аватар,
/// соцсети) сюда по-прежнему не входит — он живёт своей жизнью и грузится через
/// <c>IUserRepository</c>.
/// </param>
/// <param name="DenialStatus">
/// Причина отказа. Показывать её можно не всем — фильтруется снаружи по
/// <see cref="AccessArguments.CanViewDenialStatus"/>.
/// </param>
/// <param name="MasterAcceptedDate">Когда мастер утвердил заявку; <c>null</c>, если не утверждал.</param>
/// <param name="MasterDeclinedDate">Когда мастер отклонил заявку; <c>null</c>, если не отклонял.</param>
/// <param name="PlayerDeclinedDate">Когда игрок отозвал заявку; <c>null</c>, если не отзывал.</param>
/// <param name="CheckInDate">Когда игрока зарегистрировали на игре; <c>null</c>, если не регистрировали.</param>
/// <param name="LastMasterCommentAt">Последний мастерский комментарий, включая невидимые игроку.</param>
/// <param name="LastVisibleMasterCommentAt">Последний мастерский комментарий, видимый игроку.</param>
/// <param name="CommentDiscussionId">
/// Обсуждение заявки. Нужно, например, счётчику непрочитанных комментариев в списке заявок: сами
/// комментарии агрегат не несёт (ADR013), но связать заявку с её обсуждением должен уметь.
/// </param>
/// <param name="Finance">
/// Финансовые факты заявки: зафиксированный взнос, льгота, уплаченное, стоимость проживания,
/// факт операций, ждущих модерации. Сгруппированы отдельным типом, потому что ими пользуются
/// вместе — расчёт баланса и фильтры финансовых проблем.
/// </param>
/// <param name="AccommodationTypeId">
/// Тип проживания, выбранный в заявке, или <c>null</c>, если проживание не выбрано. Отдельно от
/// <paramref name="Finance"/>: там стоимость проживания — финансовый факт, а здесь выбор игрока,
/// по которому показывается название типа. Сам тип — настройка проекта (ADR015), поэтому только
/// идентификатор: название берётся из <c>ProjectInfo.AccommodationSettings</c>. Комнаты здесь нет —
/// она оперативные данные и живёт в агрегате поселения <c>RoomCategoryPlan</c> (ADR018).
/// </param>
/// <param name="AccommodationGroupId">
/// Группа проживающих, в которой состоит заявка (ADR022). Не опциональна: у заявки без группы это
/// она сама как одиночка. Тип задан ⇔ ссылка указывает на сложившуюся группу — тип хранится только
/// в строке группы, а выбор типа всегда заводит её, даже для одиночки. Состав группы и комнату
/// агрегат не несёт: их снимок даёт <c>RoomCategoryPlan</c>.
/// </param>
/// <param name="PlayerAllowedSensitiveData">
/// Игрок разрешил мастерам видеть свои чувствительные данные (паспорт, адрес регистрации).
/// Согласие даётся на уровне заявки, поэтому это факт о заявке, а не о профиле.
/// В БД колонка называется <c>PlayerAllowedSenstiveData</c> — с опечаткой; в доменном типе
/// имя намеренно пишется правильно, расхождение закрывается в маппере.
/// </param>
/// <param name="Fields">
/// Слой значений полей этой заявки. Хранится для каждой заявки, а не только для утверждённой:
/// без него нельзя ни показать поля заявки в обсуждении, ни посчитать её <c>ClaimFieldsFee</c>.
/// </param>
public record class CharacterClaimInfo(
    ClaimIdentification ClaimId,
    UserInfoHeader Player,
    ClaimStatus Status,
    ClaimDenialReason? DenialStatus,
    UserIdentification ResponsibleMasterId,
    DateTime CreateDate,
    DateTime LastUpdateDateTime,
    DateTime? MasterAcceptedDate,
    DateTime? MasterDeclinedDate,
    DateTime? PlayerDeclinedDate,
    DateTime? CheckInDate,
    DateTimeOffset? LastPlayerCommentAt,
    DateTimeOffset? LastMasterCommentAt,
    DateTimeOffset? LastVisibleMasterCommentAt,
    CommentDiscussionId CommentDiscussionId,
    ClaimFinanceInfo Finance,
    AccommodationTypeIdentification? AccommodationTypeId,
    AccommodationGroupIdentification AccommodationGroupId,
    bool PlayerAllowedSensitiveData,
    FieldLayerContainer Fields)
{
    /// <summary>
    /// Группа проживающих заявки. Свойство объявлено явно ради проверки инварианта «тип задан ⇔
    /// ссылка на группу» при создании.
    /// </summary>
    public AccommodationGroupIdentification AccommodationGroupId { get; init; }
        = EnsureAccommodationGroup(ClaimId, AccommodationTypeId, AccommodationGroupId);

    private static AccommodationGroupIdentification EnsureAccommodationGroup(
        ClaimIdentification claimId,
        AccommodationTypeIdentification? typeId,
        AccommodationGroupIdentification groupId)
    {
        ArgumentNullException.ThrowIfNull(groupId);

        if (groupId.ProjectId != claimId.ProjectId)
        {
            throw new ArgumentException(
                $"Accommodation group {groupId} of claim {claimId} belongs to another project", nameof(groupId));
        }

        if (typeId is not null && groupId.AsAccommodationRequestId() is null)
        {
            throw new ArgumentException(
                $"Claim {claimId} has accommodation type {typeId}, but no accommodation group", nameof(groupId));
        }

        // Без типа заявка — одиночка и ссылается сама на себя: не на группу и не на чужую заявку.
        if (typeId is null && groupId.AsClaimId() != claimId)
        {
            throw new ArgumentException(
                $"Claim {claimId} has no accommodation type and must reference itself, not {groupId}",
                nameof(groupId));
        }

        return groupId;
    }

    /// <summary>Игрок, подавший заявку.</summary>
    public UserIdentification PlayerId => Player.UserId;

    /// <summary>Заявка утверждена (в том числе если игрок уже зарегистрирован на игре).</summary>
    public bool IsApproved => Status is ClaimStatus.Approved or ClaimStatus.CheckedIn;

    /// <summary>С заявкой ещё идёт работа.</summary>
    public bool IsActive => Status.IsActive();

    /// <summary>Заявка в обсуждении: решение по ней ещё не принято.</summary>
    public bool IsInDiscussion => Status is ClaimStatus.AddedByMaster or ClaimStatus.AddedByUser or ClaimStatus.Discussed;

    /// <summary>Заявка не отклонена — ни мастером, ни игроком.</summary>
    public bool IsPending => Status is not (ClaimStatus.DeclinedByMaster or ClaimStatus.DeclinedByUser);
}
