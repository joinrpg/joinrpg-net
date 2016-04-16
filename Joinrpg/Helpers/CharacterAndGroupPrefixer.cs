using System.Collections.Generic;
using System.Linq;
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
  }
}
