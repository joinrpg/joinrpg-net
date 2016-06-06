using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public class CharacterFieldInfo
  {
    public CharacterFieldInfo(int fieldId, string fieldValue)
    {
      FieldId = fieldId;
      FieldValue = fieldValue;
    }

    public int FieldId { get; }
    public string FieldValue { get; }
  }
}
