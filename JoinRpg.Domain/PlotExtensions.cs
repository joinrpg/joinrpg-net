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

    public static bool ShouldShowPlot([NotNull] this Character character, [NotNull] PlotElement p)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      if (p == null) throw new ArgumentNullException(nameof(p));

      var groups = character.GetParentGroups().Select(g => g.CharacterGroupId);
      return p.TargetCharacters.Any(c => c.CharacterId == character.CharacterId) ||
             p.TargetGroups.Any(g => groups.Contains(g.CharacterGroupId));
    }
  }
}
