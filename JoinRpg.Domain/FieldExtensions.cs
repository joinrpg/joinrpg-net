using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class FieldExtensions
  {
    public static bool HasValueList(this ProjectCharacterField field)
    {
      return field.FieldType == CharacterFieldType.Dropdown;
    }

    public static ProjectCharacterFieldDropdownValue GetDropdownValueOrDefault(this CharacterFieldValue field)
    {
      return field.Field.DropdownValues.SingleOrDefault(
        v => v.ProjectCharacterFieldDropdownValueId.ToString() == field.Value);
    }
  }
}
