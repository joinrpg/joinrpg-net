using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class FieldExtensions
  {
    public static bool HasValueList(this ProjectCharacterField field)
    {
      return field.FieldType == CharacterFieldType.Dropdown;
    }
  }
}
