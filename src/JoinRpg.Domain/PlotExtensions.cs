using JoinRpg.DataModel;

namespace JoinRpg.Domain;

public static class PlotExtensions
{
    public static IEnumerable<IWorldObject> GetTargets(this PlotElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return element.TargetCharacters.Cast<IWorldObject>().Union(element.TargetGroups);
    }

    public static PlotElement[] SelectPlots(this Character character, IEnumerable<PlotElement> selectMany)
    {
        ArgumentNullException.ThrowIfNull(character);

        ArgumentNullException.ThrowIfNull(selectMany);

        var groups = character.GetParentGroupsToTop().Select(g => g.CharacterGroupId);
        return selectMany
          .Where(
            p => p.TargetCharacters.Any(c => c.CharacterId == character.CharacterId) ||
                 p.TargetGroups.Any(g => groups.Contains(g.CharacterGroupId))).ToArray();
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

    //TODO consider return NUll if deleted
    public static PlotElementTexts? PublishedVersion(this PlotElement e) => e.Published != null ? e.SpecificVersion((int)e.Published) : null;

    public static PlotElementTexts? PrevVersion(this PlotElement e) => e.Texts.OrderByDescending(text => text.Version).Skip(1).FirstOrDefault();
}
