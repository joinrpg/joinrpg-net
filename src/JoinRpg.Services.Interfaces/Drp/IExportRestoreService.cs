namespace JoinRpg.Services.Interfaces.Drp;

/// <summary>
/// Каким персонаж был по выгрузке мастера (DRP, #5529). null — не менять.
/// </summary>
/// <param name="Groups">
/// Обычные группы персонажа — итоговый список, а не добавка. Спецгруппы (по вариантам полей) сюда не
/// передаются: они пересчитываются по значениям полей.
/// </param>
/// <param name="Fields">
/// Значения полей персонажа — дельта: поля, которых нет в словаре, сохраняют текущее значение. null или
/// пустая строка в значении — очистить поле. Только поля персонажа: поле заявки — ошибка
/// (<see cref="ArgumentException"/>), их восстанавливает <see cref="IExportRestoreService.ApplyRestoredClaimFields"/>.
/// </param>
public record RestoredCharacterTarget(
    IReadOnlyCollection<CharacterGroupIdentification>? Groups,
    IReadOnlyDictionary<ProjectFieldIdentification, string?>? Fields);

/// <summary>
/// Восстановление значений полей, состава групп персонажей и полей заявок из выгрузки мастера после
/// потери основной БД (DRP, #5529). Только в заблокированном проекте (ADR023), без уведомлений.
/// </summary>
/// <remarks>
/// Поля проекта мастер заводит заново до загрузки выгрузки; значения пишутся в режиме восстановления —
/// как были: пустое обязательное поле не ошибка, значения по умолчанию не генерируются. Права мастера на
/// поле и проверка значений (варианты) остаются.
/// </remarks>
public interface IExportRestoreService
{
    /// <summary>
    /// Приводит персонажа к состоянию по выгрузке: обычные группы и заданные поля. Повторный вызов с тем
    /// же состоянием ничего не меняет.
    /// </summary>
    Task ApplyRestoredCharacter(CharacterIdentification characterId, RestoredCharacterTarget target);

    /// <summary>
    /// Записывает в заявку значения полей по выгрузке (дельта, как в
    /// <see cref="RestoredCharacterTarget.Fields"/>) и отмечает это скрытой записью в истории заявки без
    /// уведомления. Повторный вызов с теми же значениями ничего не меняет и в историю не пишет.
    /// </summary>
    /// <remarks>
    /// У непринятой заявки поля персонажа, совпадающие со значением у самого персонажа, в заявку не пишутся
    /// (это не предложение игрока, а значение персонажа, в том числе непубличное); остальные пишутся в
    /// заявку. Персонаж меняется только у принятой заявки.
    /// </remarks>
    Task ApplyRestoredClaimFields(
        ClaimIdentification claimId,
        IReadOnlyDictionary<ProjectFieldIdentification, string?> fields);
}
