using JoinRpg.PrimitiveTypes.Characters;

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

    public override string ToString() => $"({New.Field.Name}={Previous.Value} → {New.Value})";
}
