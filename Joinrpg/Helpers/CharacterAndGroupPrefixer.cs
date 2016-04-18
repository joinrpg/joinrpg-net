using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Helpers
{
  public static class CharacterAndGroupPrefixer
  {
    public const string GroupFieldPrefix = "group^";
    public const string CharFieldPrefix = "character^";

    public static IEnumerable<string> PrefixAsGroups(this IEnumerable<int> groupIds)
      => groupIds.Select(id => $"'{GroupFieldPrefix}{id}'");

    public static IEnumerable<string> PrefixAsGroups(this int groupId)
      => new[] {groupId}.PrefixAsGroups();

    public static IEnumerable<string> PrefixAsCharacters(this IEnumerable<int> characterId)
      => characterId.Select(id => $"'{CharFieldPrefix}{id}'");

    public static IEnumerable<string> PrefixAsCharacters(this int characterId)
      => new[] { characterId }.PrefixAsCharacters();

    public static List<int> GetUnprefixedChars(this IEnumerable<string> targets)
    {
      return targets.UnprefixNumbers(CharFieldPrefix).ToList();
    }

    public static List<int> GetUnprefixedGroups(this IEnumerable<string> targets)
    {
      return targets.UnprefixNumbers(GroupFieldPrefix).ToList();
    }

    public static List<string> GetParentGroupsForEdit(this IWorldObject field)
    {
      return field.ParentGroups.Where(gr => !gr.IsSpecial).Select(pg => pg.CharacterGroupId).PrefixAsGroups().ToList();
    }

    public static List<string> AsPossibleParentForEdit(this CharacterGroup field)
    {
      return new List<int> {field.CharacterGroupId}.PrefixAsGroups().ToList();
    }

    public static IEnumerable<string> GetElementBindingsForEdit(this PlotElement e)
    {
      return e.TargetGroups.Select(g => g.CharacterGroupId).PrefixAsGroups().Union(e.TargetCharacters.Select(c => c.CharacterId).PrefixAsCharacters());
    }
  }
}
