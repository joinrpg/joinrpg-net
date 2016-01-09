using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class FieldExtensions
  {
    public static bool HasValueList(this ProjectCharacterField field)
    {
      return field.FieldType == CharacterFieldType.Dropdown || field.FieldType == CharacterFieldType.MultiSelect;
    }

    public static IReadOnlyList<ProjectCharacterFieldDropdownValue> GetDropdownValues(this CharacterFieldValue field)
    {
      var value = field.GetSelectedIds();
      return field.GetPossibleValues().Where(
        v => value.Contains(v.ProjectCharacterFieldDropdownValueId)).ToList().AsReadOnly();
    }

    private static IEnumerable<int> GetSelectedIds(this CharacterFieldValue field)
    {
      return string.IsNullOrWhiteSpace(field.Value) ? Enumerable.Empty<int>() : field.Value.Split(',').Select(int.Parse);
    }

    public static IEnumerable<ProjectCharacterFieldDropdownValue> GetPossibleValues(this CharacterFieldValue field)
      => field.Field.GetOrderedValues();

    public static string GetSpecialGroupName(this ProjectCharacterFieldDropdownValue fieldValue)
    {
      return $"${fieldValue.ProjectCharacterField.FieldName} = {fieldValue.Label}";
    }

    public static string GetSpecialGroupName(this ProjectCharacterField field)
    {
      return $"${field.FieldName}";
    }
  }
}
