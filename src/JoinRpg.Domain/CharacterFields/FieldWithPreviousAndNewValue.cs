using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.CharacterFields;

/// <summary>
/// A class to store project field's previous and new value
/// </summary>
public class FieldWithPreviousAndNewValue(
  ProjectFieldInfo field,
  string? value,
  string? previousValue) : FieldWithValue(field, value)
{
    public string? PreviousValue { get; private set; } = previousValue;

    public string PreviousDisplayString =>
      GetDisplayValue(PreviousValue, Field.HasValueList ? PreviousValue?.ParseToIntList() ?? [] : []);

    public override string ToString() => $"{Field.Name}={Value},PreviousValue={PreviousValue}";
}
