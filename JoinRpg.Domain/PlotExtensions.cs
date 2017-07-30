using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class PlotExtensions
  {
    public static IEnumerable<IWorldObject> GetTargets([NotNull] this PlotElement element)
    {
      if (element == null) throw new ArgumentNullException(nameof(element));
      return element.TargetCharacters.Cast<IWorldObject>().Union(element.TargetGroups);
    }

    [NotNull, ItemNotNull]
    public static PlotElement[] SelectPlots([NotNull] this Character character, [NotNull] IEnumerable<PlotElement> selectMany)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      if (selectMany == null) throw new ArgumentNullException(nameof(selectMany));

      var groups = character.GetParentGroupsToTop().Select(g => g.CharacterGroupId);
      return selectMany
        .Where(
          p => p.TargetCharacters.Any(c => c.CharacterId == character.CharacterId) ||
               p.TargetGroups.Any(g => groups.Contains(g.CharacterGroupId))).ToArray();
    }

    public static int CountCharacters([NotNull] this PlotElement element,
      [NotNull, ItemNotNull] IReadOnlyCollection<Character> characters)
    {
      if (element == null) throw new ArgumentNullException(nameof(element));
      if (characters == null) throw new ArgumentNullException(nameof(characters));

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

    [NotNull]
    public static PlotElementTexts LastVersion([NotNull] this PlotElement e)
    {
      return e.Texts.OrderByDescending(text => text.Version).First();
    }

    [CanBeNull]
    public static PlotElementTexts SpecificVersion([NotNull] this PlotElement e, int version)
    {
      return e.Texts.SingleOrDefault(text => text.Version == version);
    }

    //TODO consider return NUll if deleted
    [CanBeNull]
    public static PlotElementTexts PublishedVersion([NotNull] this PlotElement e)
    {
      return e.Published != null ? e.SpecificVersion((int) e.Published) : null;
    }

    [CanBeNull]
    public static PlotElementTexts PrevVersion(this PlotElement e)
    {
      return e.Texts.OrderByDescending(text => text.Version).Skip(1).FirstOrDefault();
    }
  }
}
