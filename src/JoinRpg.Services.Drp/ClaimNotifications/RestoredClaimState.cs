using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Drp.ClaimNotifications;

/// <summary>
/// В чём восстановление по уведомлениям не уверено — мастер решает сам.
/// </summary>
public enum RestoredClaimWarning
{
    /// <summary>Игрока не удалось определить ни по приветствиям, ни по действиям «игроком».</summary>
    PlayerNotFound,

    /// <summary>Под имя игрока подходит несколько пользователей.</summary>
    PlayerAmbiguous,
}

/// <summary>
/// Что известно о заявке по уведомлениям до момента аварии. null — уведомления об этом ничего не говорят.
/// </summary>
/// <param name="CharacterName">Имя персонажа из последнего события.</param>
/// <param name="MayHaveChangedCharacter">
/// Было перемещение, восстановление или одобрение (одобрение слота создаёт нового персонажа) —
/// персонаж из последнего заголовка может быть уже не тем, что в базе. Без таких событий другое имя —
/// это переименование.
/// </param>
/// <param name="Player">Игрок, если определён однозначно.</param>
/// <param name="PlayerCandidates">Все подходящие пользователи — для выбора мастером при неоднозначности.</param>
/// <param name="IsNewClaim">Есть событие «Новая заявка»: заявка подана после бэкапа.</param>
/// <param name="Status">Статус после последнего статусного события.</param>
/// <param name="StatusChangedAt">Когда статус стал таким.</param>
/// <param name="ResponsibleMasterName">Отображаемое имя ответственного после последней смены.</param>
/// <param name="ManualFee">Взнос, последний раз установленный вручную.</param>
/// <param name="FirstActivity">Время первого события.</param>
/// <param name="LastActivity">Время последнего события.</param>
public record RestoredClaimState(
    ClaimIdentification ClaimId,
    string? CharacterName,
    bool MayHaveChangedCharacter,
    UserIdentification? Player,
    IReadOnlyCollection<UserIdentification> PlayerCandidates,
    bool IsNewClaim,
    ClaimStatus? Status,
    DateTimeOffset? StatusChangedAt,
    string? ResponsibleMasterName,
    DateTimeOffset? ResponsibleChangedAt,
    int? ManualFee,
    DateTimeOffset FirstActivity,
    DateTimeOffset LastActivity,
    IReadOnlyCollection<RestoredClaimWarning> Warnings);
