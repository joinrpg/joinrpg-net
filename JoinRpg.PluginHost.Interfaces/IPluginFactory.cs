using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.PluginHost.Interfaces
{
  public sealed class ProjectPluginInfo
  {
    public ProjectPluginInfo(string name, bool installed, IReadOnlyCollection<string> staticPages, string description)
    {
      Name = name;
      Installed = installed;
      StaticPages = staticPages;
      Description = description;
    }

    public string Name { get; }
    public bool Installed { get; }
    public IReadOnlyCollection<string> StaticPages { get; }
    public string Description { get;  }
  }

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

  public interface IPluginFactory
  {
    Task<IEnumerable<PluginOperationData<T>>> GetPossibleOperations<T>(int projectId) where T:IPluginOperation;

    [ItemCanBeNull]
    Task<PluginOperationData<T>> GetOperationInstance<T>(int projectid, string plugin) where T : IPluginOperation;

    IEnumerable<HtmlCardPrintResult> PrintForCharacter(PluginOperationData<IPrintCardPluginOperation> pluginInstance, Character c);

    [NotNull]
    MarkdownString ShowStaticPage(PluginOperationData<IStaticPagePluginOperation> pluginInstance, Project project);

    [ItemNotNull]
    Task<IReadOnlyCollection<ProjectPluginInfo>> GetPluginsForProject(int projectId);

    [ItemCanBeNull]
    Task<ProjectPluginConfiguraton> GetConfiguration(int projectid, string plugin);
  }
}