using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Interfaces.Drp;

/// <summary>
/// Каким заявка была на момент потери основной БД — по базе уведомлений (DRP, #5481). null — не менять.
/// </summary>
/// <param name="Character">Персонаж, на котором заявка оказалась (перемещение после бэкапа).</param>
/// <param name="Status">Статус после последнего статусного события.</param>
/// <param name="StatusChangedAt">Когда статус стал таким — им проставляется отметка даты статуса.</param>
/// <param name="ResponsibleMaster">Ответственный мастер; обязан быть мастером проекта.</param>
/// <param name="ManualFee">Взнос, установленный вручную.</param>
public record RestoredClaimTarget(
    CharacterIdentification? Character,
    ClaimStatus? Status,
    DateTimeOffset? StatusChangedAt,
    UserIdentification? ResponsibleMaster,
    int? ManualFee);

/// <summary>
/// Восстановление заявок по базе уведомлений после потери основной БД (DRP-план, фаза 7, #5481).
/// Только в заблокированном проекте (ADR023), без уведомлений, с записью в истории заявки.
/// </summary>
public interface IClaimRestoreService
{
    /// <summary>
    /// Приводит заявку, которая есть в восстановленной базе, к состоянию на момент аварии. Повторный
    /// вызов с тем же состоянием ничего не меняет и в историю не пишет.
    /// </summary>
    Task ApplyRestoredState(ClaimIdentification claimId, RestoredClaimTarget target);
}
