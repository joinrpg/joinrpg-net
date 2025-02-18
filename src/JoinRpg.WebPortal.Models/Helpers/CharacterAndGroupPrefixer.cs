using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Helpers;

public static class CharacterAndGroupPrefixer
{
    public const string GroupFieldPrefix = "group^";
    public const string CharFieldPrefix = "character^";

    public static IEnumerable<string> PrefixAsGroups(this IEnumerable<int> groupIds)
      => groupIds.Select(id => $"'{GroupFieldPrefix}{id}'");

    public static IEnumerable<string> PrefixAsGroups(this int groupId)
      => new[] { groupId }.PrefixAsGroups();

    private static IEnumerable<string> PrefixAsCharacters(this IEnumerable<int> characterId)
      => characterId.Select(id => $"'{CharFieldPrefix}{id}'");

    public static List<CharacterIdentification> GetUnprefixedChars(this IEnumerable<string> targets, ProjectIdentification projectIdentification)
    {
        ArgumentNullException.ThrowIfNull(targets);

        return [.. CharacterIdentification.FromList(targets.UnprefixNumbers(CharFieldPrefix), projectIdentification)];
    }

    public static List<CharacterGroupIdentification> GetUnprefixedGroups(this IEnumerable<string> targets, ProjectIdentification projectIdentification)
    {
        ArgumentNullException.ThrowIfNull(targets);

        return [.. CharacterGroupIdentification.FromList(targets.UnprefixNumbers(GroupFieldPrefix), projectIdentification)];
    }

    public static List<string> GetParentGroupsForEdit(this IWorldObject field) => field.ParentGroups.Where(gr => !gr.IsSpecial).Select(pg => pg.CharacterGroupId).PrefixAsGroups().ToList();

    public static List<string> AsPossibleParentForEdit(this CharacterGroup field) => new List<int> { field.CharacterGroupId }.PrefixAsGroups().ToList();

    public static IEnumerable<string> GetElementBindingsForEdit(this PlotElement e) => e.TargetGroups.Select(g => g.CharacterGroupId).PrefixAsGroups().Union(e.TargetCharacters.Select(c => c.CharacterId).PrefixAsCharacters());
}
