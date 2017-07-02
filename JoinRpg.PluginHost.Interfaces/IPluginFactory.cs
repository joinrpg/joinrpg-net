using System.Collections.Generic;
using System.Linq;
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
    IEnumerable<HtmlCardPrintResult> PrintForCharacter(PluginOperationData<IPrintCardPluginOperation> pluginInstance, Character c);

    [NotNull]
    MarkdownString ShowStaticPage(PluginOperationData<IStaticPagePluginOperation> pluginInstance, Project project);

    [ItemNotNull]
    Task<IReadOnlyCollection<ProjectPluginInfo>> GetPluginsForProject(int projectId);

    [ItemCanBeNull]
    Task<ProjectPluginConfiguraton> GetConfiguration(int projectid, string plugin);

    [NotNull, ItemNotNull]
    IEnumerable<PluginOperationData<T>> GetProjectOperations<T>(Project project) where T : IPluginOperation;

    string GenerateDefaultCharacterFieldValue(ProjectField field);
  }

  public static class IPluginFactoryExtenstions
  {
    [CanBeNull]
    public static PluginOperationData<T> GetOperationInstance<T>(this IPluginFactory self, Project project, string plugin)
      where T : IPluginOperation
    {
      return self.GetProjectOperations<T>(project).SingleOrDefault(p => p.OperationName == plugin);
    }
  }
}