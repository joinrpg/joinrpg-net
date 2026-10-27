using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

/// <summary>
/// A class to store project field's previous and new value
/// </summary>
public record class FieldWithPreviousAndNewValue
{
    public FieldWithValue New { get; }
    public FieldWithValue Previous { get; }
    public ProjectFieldInfo Field => New.Field;
    /// <param name="current">Поле со значением ДО изменения.</param>
    public FieldWithPreviousAndNewValue(FieldWithValue current, string? newValue)
    {
        New = current.WithValue(newValue);
        Previous = current;
    }
    public string? PreviousValue => Previous.Value;

    public string PreviousDisplayString => Previous.DisplayString;

    public override string ToString() => $"({New.Field.Name}={Previous.Value} → {New.Value})";
}
