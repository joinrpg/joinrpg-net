using JoinRpg.DataModel;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  public class DetectiveConfiguration : IShowConfigurationPluginOperation
  {
    private readonly string _config;

    public DetectiveConfiguration(string config)
    {
      _config = config;
    }

    public MarkdownString ShowPluginConfiguration()
    {
      return new MarkdownString(_config);
    }
  }
}