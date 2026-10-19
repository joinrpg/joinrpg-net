using JoinRpg.DomainTypes.Characters;

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

    /// <summary>
    /// Возвращает потерянную группу с прежним идентификатором: непубличную (видимость в выгрузке не
    /// известна — скрытая ничего не раскроет), прямо под корневой группой. Если группа с таким id в этом
    /// проекте уже есть — ничего не делает.
    /// </summary>
    /// <remarks>
    /// Группы восстанавливаются раньше персонажей: персонаж ссылается на них. Метаданные проекта после
    /// вставки перечитываются, поэтому новая группа видна последующему <see cref="RestoreLostCharacter"/>
    /// и <see cref="ApplyRestoredCharacter"/> и в том же запросе.
    /// </remarks>
    /// <exception cref="JoinValidationException">
    /// Id занят группой другого проекта или не ниже счётчика (такого id база не выдавала — это не
    /// потерянная группа).
    /// </exception>
    Task RestoreLostGroup(CharacterGroupIdentification groupId, string name);

    /// <summary>
    /// Возвращает потерянного персонажа с прежним идентификатором и приводит его к состоянию по
    /// выгрузке. Новый персонаж — заданных типа и видимости, в корневой группе (как при восстановлении
    /// заявки), дальше — как <see cref="ApplyRestoredCharacter"/>. Если персонаж с таким id в этом
    /// проекте уже есть — только <see cref="ApplyRestoredCharacter"/>, тип и видимость не меняются.
    /// </summary>
    /// <param name="characterId">Прежний идентификатор персонажа.</param>
    /// <param name="name">
    /// Имя персонажа. Если в проекте настроено поле-имя, имя пишется его значением (если значения этого
    /// поля нет в <paramref name="target"/>; если есть — главнее оно); иначе — самим именем персонажа,
    /// и тогда у уже существующего персонажа имя не меняется.
    /// </param>
    /// <param name="characterType">Тип нового персонажа (по «Занят?» выгрузки, #5645). Лимита у шаблона нет.</param>
    /// <param name="isHot">Горячий ли новый персонаж; у NPC не бывает (<see cref="ArgumentException"/>).</param>
    /// <param name="visibility">
    /// Видимость нового персонажа: в выгрузке её нет, выбирает мастер (#5677). У уже существующего
    /// персонажа не меняется.
    /// </param>
    /// <param name="target">Состояние по выгрузке, как у <see cref="ApplyRestoredCharacter"/>.</param>
    /// <remarks>
    /// Вставка и наполнение — без общей транзакции, но повторяемы: повтор после сбоя между ними найдёт
    /// вставленного персонажа и доведёт его до состояния по выгрузке.
    /// </remarks>
    /// <exception cref="JoinValidationException">
    /// Id занят персонажем другого проекта или не ниже счётчика.
    /// </exception>
    Task RestoreLostCharacter(
        CharacterIdentification characterId,
        string name,
        CharacterType characterType,
        bool isHot,
        CharacterVisibility visibility,
        RestoredCharacterTarget target);

    /// <summary>
    /// Открывает скрытые группы, созданные восстановлением, в которых лежит видимый всем персонаж
    /// (публичный или со скрытым игроком), — по <c>RestoredGroupPublication</c> (#5677). Скрытый
    /// персонаж групп не открывает; скрытые группы из резервной копии не трогаются.
    /// </summary>
    /// <param name="backupTakenAt">
    /// Момент резервной копии: группы, созданные позже, считаются созданными восстановлением.
    /// </param>
    /// <returns>Открытые группы; пусто — открывать было нечего.</returns>
    Task<IReadOnlyList<CharacterGroupIdentification>> PublishRestoredGroupsOf(CharacterIdentification characterId, DateTimeOffset backupTakenAt);
}
