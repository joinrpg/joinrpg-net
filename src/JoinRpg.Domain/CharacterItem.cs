using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain;

/// <summary>
/// Обёртка над EF-сущностью для проверок доступности поля.
/// </summary>
/// <remarks>
/// Временная: проблемы и персонажа, и заявки уже считаются по доменному агрегату
/// <see cref="CharacterInfo"/> (ADR013) — он реализует этот интерфейс сам, без обёртки. Здесь она
/// остаётся только для путей, ещё сидящих на EF-графе: сохранения полей
/// (<c>CharacterExistsStrategyBase</c>) и <c>CustomFieldsViewModels</c>. Кеширующего загрузчика
/// <c>CharacterBulkLoader</c> к ней больше нет — он был нужен только валидатору проблем.
/// </remarks>
public record class CharacterItem(Character Character, IReadOnlyCollection<CharacterGroupIdentification> ParentGroups)
    : IFieldAvailabilityTarget
{
    CharacterType IFieldAvailabilityTarget.CharacterType => Character.CharacterType;

    IReadOnlyCollection<CharacterGroupIdentification> IFieldAvailabilityTarget.ParentGroupIdsToTop => ParentGroups;
}
