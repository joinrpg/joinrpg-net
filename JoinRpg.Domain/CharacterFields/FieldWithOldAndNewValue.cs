using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain.CharacterFields
{
  /// <summary>
  /// A class to store project field's previous and new value
  /// </summary>
  public class FieldWithOldAndNewValue : FieldWithValue
  {
    public string PreviousValue { get; private set; }

    public string PreviousDisplayValue => GetDisplayValue(PreviousValue, PreviousValue.ToIntList());

    public FieldWithOldAndNewValue(
      ProjectField field,
      [CanBeNull] string value,
      [CanBeNull] string previousValue) : base(field, value)
    {
      PreviousValue = previousValue;
    }

    public FieldWithOldAndNewValue(
      FieldWithValue fieldWithValue,
      string previousValue) :
        this(fieldWithValue.Field, fieldWithValue.Value, previousValue)
    {
    }

    public override string ToString() => $"{Field.FieldName}={Value},PreviousValue={PreviousValue}";
  }
}
