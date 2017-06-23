using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  //TODO That's incorrect place, move to PluginHost
  //TODO Configuration show/edit should be implemented by pluginhost directly
  internal class ShowRawJsonConfiguration : IShowConfigurationPluginOperation
  {
    private readonly string _config;

    public ShowRawJsonConfiguration(PluginConfiguration config)
    {
      _config = config.ConfigurationString;
    }

    public MarkdownString ShowPluginConfiguration(IEnumerable<CharacterGroupInfo> projectGroups, IEnumerable<ProjectFieldInfo> fields)
    {
      return new MarkdownString($"```\n{_config}\n```");
    }
  }
}