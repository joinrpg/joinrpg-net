using System.Collections.Generic;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public interface IShowConfigurationPluginOperation: IPluginOperation
  {
    MarkdownString ShowPluginConfiguration(IEnumerable<CharacterGroupInfo> projectGroups, IEnumerable<ProjectFieldInfo> fields);
  }
}