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
  }
}
