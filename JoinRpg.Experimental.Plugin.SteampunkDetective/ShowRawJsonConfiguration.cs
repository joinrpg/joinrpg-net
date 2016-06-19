using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  //TODO Maybe we should implement it directly in PluginHost
  public class ShowRawJsonConfiguration : IShowConfigurationPluginOperation
  {
    private readonly string _config;

    public ShowRawJsonConfiguration(string config)
    {
      _config = config;
    }

    public MarkdownString ShowPluginConfiguration(IEnumerable<CharacterGroupInfo> projectGroups, IEnumerable<ProjectFieldInfo> fields)
    {
      return new MarkdownString($"```\n{_config}\n```");
    }
  }
}