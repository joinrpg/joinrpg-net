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

    public static IEnumerable<ProjectCharacterFieldDropdownValue> GetDropdownValues(this CharacterFieldValue field)
    {
      if (string.IsNullOrWhiteSpace(field.Value))
      {
        return Enumerable.Empty<ProjectCharacterFieldDropdownValue>();
      }
      var value = field.Value.Split(',').Select(int.Parse);
      return field.Field.DropdownValues.Where(
        v => value.Contains(v.ProjectCharacterFieldDropdownValueId));
    }

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
