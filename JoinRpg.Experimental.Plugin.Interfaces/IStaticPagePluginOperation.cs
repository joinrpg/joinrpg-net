using System.Collections.Generic;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public interface IStaticPagePluginOperation: IPluginOperation
  {
    [NotNull]
    MarkdownString ShowStaticPage(IEnumerable<CharacterGroupInfo> projectGroups, IEnumerable<ProjectFieldInfo> fields);
  }
}