using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Domain;

public static class PlotExtensions
{
    public static IEnumerable<ILinkableWithName> GetTargetLinks(this PlotElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return element.TargetCharacters.Cast<ILinkableWithName>().Union(element.TargetGroups);
    }

    public static int CountCharacters(this PlotElement element, IReadOnlyCollection<Character> characters)
    {
        ArgumentNullException.ThrowIfNull(element);

        ArgumentNullException.ThrowIfNull(characters);

        return characters.Count(character =>
        {
            if (element.TargetCharacters.Any(c => c.CharacterId == character.CharacterId))
            {
                return true;
            }
            if (element.TargetGroups.Any(g => g.CharacterGroupId == element.Project.RootGroup.CharacterGroupId))
            {
                return true;
            }
            var groups = character.GetParentGroupsToTop().Select(g => g.CharacterGroupId);
            return element.TargetGroups.Any(g => groups.Contains(g.CharacterGroupId));
        });
    }

    public static PlotElementTexts LastVersion(this PlotElement e) => e.Texts.OrderByDescending(text => text.Version).First();

    public static PlotElementTexts? SpecificVersion(this PlotElement e, int version) => e.Texts.SingleOrDefault(text => text.Version == version);


    public static TargetsInfo ToTarget(this PlotElement element)
    {
        return new TargetsInfo(
                            [.. element.TargetCharacters.Select(x => new CharacterTarget(x.GetId(), x.CharacterName))],
                            [.. element.TargetGroups.Select(x => new GroupTarget(x.GetId(), x.CharacterGroupName))]);
    }
}
