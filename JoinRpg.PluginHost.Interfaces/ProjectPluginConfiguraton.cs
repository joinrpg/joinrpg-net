using JoinRpg.DataModel;

namespace JoinRpg.PluginHost.Interfaces
{
  public sealed class ProjectPluginConfiguraton
  {
    public ProjectPluginConfiguraton(string name, MarkdownString configuration)
    {
      Name = name;
      Configuration = configuration;
    }

    public string Name { get; }
    public MarkdownString Configuration { get; }
  }
}