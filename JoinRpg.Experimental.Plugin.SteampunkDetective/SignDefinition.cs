using System;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  [PublicAPI]
  public class SignDefinition
  {
    public int Code { get; set; }
    public int FieldId { get; set; }
    [NotNull, ItemNotNull]
    public string[] AllowedValues { get; set; } = {};
    [NotNull]
    public int[] ShowForGroups { get; set; } = {};
    [NotNull]
    public int[] SkipForGroups { get; set; } = {};

    public int Weight { get; set; } = 1;

    public bool IsValidForCharacter([NotNull] CharacterInfo character)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));

      var fieldValues = character.Fields.SingleOrDefault(f => f.FieldId == FieldId)?.FieldValue?.Split(',');
      var allowByField = FieldId == 0 || AllowedValues.Intersect(fieldValues ?? Enumerable.Empty<string>()).Any();
      var allowByGroup = ShowForGroups.Length == 0 ||
                         ShowForGroups.Intersect(character.Groups.Select(g => g.CharacterGroupId)).Any();
      var disallowByGroup = SkipForGroups.Intersect(character.Groups.Select(g => g.CharacterGroupId)).Any();
      return allowByField && allowByGroup && !disallowByGroup;
    }
  }
}