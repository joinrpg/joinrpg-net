using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain.CharacterFields
{
  /// <summary>
  /// A class to store project field's previous and new value
  /// </summary>
  public class FieldWithPreviousAndNewValue : FieldWithValue
  {
    public string PreviousValue { get; private set; }

    public string PreviousDisplayString =>
      GetDisplayValue(PreviousValue, Field.HasValueList() ? PreviousValue.ToIntList() : new int[0]);

    public FieldWithPreviousAndNewValue(
      ProjectField field,
      [CanBeNull] string value,
      [CanBeNull] string previousValue) : base(field, value)
    {
      PreviousValue = previousValue;
    }

    public override string ToString() => $"{Field.FieldName}={Value},PreviousValue={PreviousValue}";
  }
}
