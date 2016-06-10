using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public class CharacterFieldInfo
  {
    public CharacterFieldInfo(int fieldId, string fieldValue, string fieldName, string fieldDisplayValue)
    {
      FieldId = fieldId;
      FieldValue = fieldValue;
      FieldName = fieldName;
      FieldDisplayValue = fieldDisplayValue;
    }

    public int FieldId { get; }
    public string FieldValue { get; }

    public string FieldDisplayValue { get; }
    public string FieldName { get; }

    public override string ToString() => $"{FieldName}={FieldDisplayValue}";
  }
}
