using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using Newtonsoft.Json;

namespace JoinRpg.PluginHost.Impl
{
  [UsedImplicitly]
  public class PluginFactoryImpl : IPluginFactory
  {
    private IProjectRepository ProjectRepository { get; }
    private IReadOnlyCollection<IPlugin> Plugins { get; }
    
    public PluginFactoryImpl(IProjectRepository projectRepository, IPlugin[] plugins)
    {
      ProjectRepository = projectRepository;
      Plugins = plugins;
    }


    public async Task<IEnumerable<PluginOperationData<T>>> GetPossibleOperations<T>(int projectId)
      where T : IPluginOperation
    {
      var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
      return GetProjectOperatons<T>(project);
    }

    private IEnumerable<PluginOperationData<T>> GetProjectOperatons<T>(Project project) where T : IPluginOperation
    {
      return from projectPlugin in GetProjectInstalledPlugins(project)
        from pluginOperationMetadata in GetOperationsOfType<T>(projectPlugin.Plugin)
        select CreatePluginOperationData<T>(project, projectPlugin, pluginOperationMetadata);
    }

    private static IEnumerable<PluginOperationMetadata> GetOperationsOfType<T>(IPlugin plugin) where T : IPluginOperation
    {
      return plugin.GetOperations().Where(o => typeof(T).IsAssignableFrom(o.Operation));
    }

    private static PluginOperationData<T> CreatePluginOperationData<T>(Project project, PluginWithConfig projectPlugin,
      PluginOperationMetadata pluginOperationMetadata) where T : IPluginOperation
    {
      return new PluginOperationData<T>(
        $"{projectPlugin.Plugin.GetName()}.{pluginOperationMetadata.Name}",
        () =>
          (T) pluginOperationMetadata.CreateInstance(new PluginConfiguration(project.ProjectName, projectPlugin.Configuration)),
        pluginOperationMetadata.Description, pluginOperationMetadata.AllowPlayerAccess);
    }

    private class PluginWithConfig
    {
      public IPlugin Plugin { get; set; }
      public string Configuration { get; set; }
    }

    private IEnumerable<PluginWithConfig> GetProjectInstalledPlugins(Project project)
    {
      return project.ProjectPlugins.Join(Plugins, pp => pp.Name, p => p.GetName(),
        (pp, p) => new PluginWithConfig {Plugin = p, Configuration = pp.Configuration});
    }

    public async Task<PluginOperationData<T>> GetOperationInstance<T>(int projectid, string plugin)
      where T:IPluginOperation
    {
      return (await GetPossibleOperations<T>(projectid)).SingleOrDefault(
        p => p.OperationName == plugin);
    }

    public IEnumerable<HtmlCardPrintResult> PrintForCharacter(PluginOperationData<IPrintCardPluginOperation> pluginInstance, Character c)
    {
      return pluginInstance.CreatePluginInstance().PrintForCharacter(PrepareCharacterForPlugin(c));
    }

    public MarkdownString ShowStaticPage(PluginOperationData<IStaticPagePluginOperation> pluginInstance, Project project)
    {
      return
        pluginInstance.CreatePluginInstance()
          .ShowStaticPage(project.CharacterGroups.Select(g => new CharacterGroupInfo(g.CharacterGroupId, g.CharacterGroupName)),
            project.ProjectFields.Select(
              f =>
                new ProjectFieldInfo(f.ProjectFieldId, f.FieldName,
                  f.DropdownValues.ToDictionary(fv => fv.ProjectFieldDropdownValueId, fv => fv.Label))));
    }

    public async Task<IReadOnlyCollection<ProjectPluginInfo>> GetPluginsForProject(int projectId)
    {
      var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
      return Plugins
        .Select(plugin =>
          {
            var pluginName = plugin.GetName();
            return new ProjectPluginInfo(pluginName,
              project.ProjectPlugins.Any(pp => pp.Name == pluginName),
                GetOperationsOfType<IStaticPagePluginOperation>(plugin).Select(o => pluginName + "." + o.Name).ToList(),
                plugin.GetDescripton());
          }
        )
        .ToList();
    }

    public async Task<ProjectPluginConfiguraton> GetConfiguration(int projectid, string plugin)
    {
      var project = await ProjectRepository.GetProjectWithDetailsAsync(projectid);
      var pluginInstance = GetProjectInstalledPlugins(project).SingleOrDefault(p => p.Plugin.GetName() == plugin);
      if (pluginInstance == null) return null;
      return new ProjectPluginConfiguraton(pluginInstance.Plugin.GetName(),
        new MarkdownString($"```\n{pluginInstance.Configuration}\n```"));
    }

    private static CharacterInfo PrepareCharacterForPlugin(Character character)
    {
      var player = character.ApprovedClaim?.Player;
      return new CharacterInfo(character.CharacterName,
        character.GetFields()
          .Select(f => new CharacterFieldInfo(f.Field.ProjectFieldId, f.Value, f.Field.FieldName, f.DisplayString)),
        character.CharacterId,
        character.GetParentGroupsToTop().Distinct()
          .Where(g => g.IsActive && !g.IsSpecial && !g.IsRoot)
          .Select(g => new CharacterGroupInfo(g.CharacterGroupId, g.CharacterGroupName)),
        player?.DisplayName, player?.FullName, player?.Id);
    }
  }

  public class PluginConfiguration : IPluginConfiguration
  {
    public PluginConfiguration([NotNull] string projectName, [NotNull] string configurationString)
    {
      if (projectName == null) throw new ArgumentNullException(nameof(projectName));
      if (configurationString == null) throw new ArgumentNullException(nameof(configurationString));
      ProjectName = projectName;
      ConfigurationString = configurationString;
    }

    private string ConfigurationString { get; }

    [NotNull]
    public T GetConfiguration<T>() => JsonConvert.DeserializeObject<T>(ConfigurationString);

    [NotNull]
    public string ProjectName { get; }
  }
}