using System;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  public class SignDefinition
  {
    public int Code { get; set; }
    public SignType SignType { get; set; }
    public int FieldId { get; set; }

    [UsedImplicitly]
    public string[] AllowedValues { get; set; }

    public bool IsValidForCharacter(CharacterInfo character)
    {
      switch (SignType)
      {
        case SignType.FieldBased:
          var field = character.Fields.SingleOrDefault(f => f.FieldId == FieldId);
          return AllowedValues.Contains(field?.FieldValue);
        case SignType.GroupBased:
          return AllowedValues.Intersect(character.Groups.Select(g => g.CharacterGroupId.ToString())).Any();
        default:
          throw new ArgumentOutOfRangeException();
      }
    }
  }
}