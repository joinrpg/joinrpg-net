using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

/// <summary>
/// A class to store project field's previous and new value
/// </summary>
public record class FieldWithPreviousAndNewValue
{
    public FieldWithValue New { get; private set; }
    public FieldWithValue Previous { get; private set; }
    public ProjectFieldInfo Field => New.Field;
    public FieldWithPreviousAndNewValue(FieldWithValue current, string? newValue)
    {
        New = new FieldWithValue(current.Field, newValue);
        Previous = current;
    }
    public string? PreviousValue => Previous.Value;

    public string PreviousDisplayString => Previous.DisplayString;

    /// <summary>
    /// Отметило ли это изменение поле (или выбранный вариант) как использованное <b>впервые</b>.
    /// </summary>
    /// <remarks>
    /// Отметка <c>WasEverUsed</c> монотонна (<c>false → true</c>) и входит в метаданные проекта,
    /// запрещая окончательное удаление поля. Читается снимок: <c>FieldSaveHelper</c> мутирует
    /// EF-сущности, а не <c>ProjectInfo</c>, поэтому на момент проверки снимок ещё описывает
    /// состояние ДО операции. Если ничего нового не отмечено — а это подавляющее большинство
    /// сохранений — метаданные не изменились, и пересобирать их незачем.
    /// </remarks>
    public bool MarksNewFieldUsage
        => !Field.WasEverUsed
            || (Field.HasValueList && New.GetDropdownValues().Any(variant => !variant.WasEverUsed));

    public override string ToString() => $"({New.Field.Name}={Previous.Value} → {New.Value})";
}
