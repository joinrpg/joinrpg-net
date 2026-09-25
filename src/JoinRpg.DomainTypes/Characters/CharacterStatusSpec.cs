namespace JoinRpg.DomainTypes.Characters;

/// <summary>
/// Какие персонажи нужны запросу — по признаку «удалён / не удалён».
/// </summary>
/// <remarks>
/// Аналог <see cref="Claims.ClaimStatusSpec"/> для заявок. Больше вариантов тут нет сознательно:
/// тип персонажа (NPC, слот), занятость роли и доступность для заявки — это доменные правила
/// поверх уже загруженного персонажа, а не отбор в запросе. Попытка отобрать «доступных» SQL-ем
/// один раз уже разъехалась с правилами (см. issue #4766).
/// </remarks>
public enum CharacterStatusSpec
{
    /// <summary>Все персонажи, включая удалённых.</summary>
    Any,

    /// <summary>Только не удалённые (<c>IsActive</c>).</summary>
    Active,

    /// <summary>Только удалённые — их показывает архив.</summary>
    Deleted,
}

public static class CharacterStatusSpecExtensions
{
    /// <summary>
    /// Подходит ли персонаж с таким <paramref name="isActive"/> под спецификацию.
    /// </summary>
    /// <remarks>
    /// Зеркало <c>CharacterPredicates.ByStatus</c> для отбора в памяти — при изменении правила
    /// править оба места (согласованность закреплена тестом).
    /// </remarks>
    public static bool Matches(this CharacterStatusSpec spec, bool isActive)
        => spec switch
        {
            CharacterStatusSpec.Any => true,
            CharacterStatusSpec.Active => isActive,
            CharacterStatusSpec.Deleted => !isActive,
            _ => throw new ArgumentOutOfRangeException(nameof(spec), spec, null),
        };
}
