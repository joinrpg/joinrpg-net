using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

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

        public IEnumerable<PluginOperationData<T>> GetProjectOperations<T>(Project project) where T : IPluginOperation
        {
            return from projectPlugin in GetProjectInstalledPlugins(project)
                   from pluginOperationMetadata in projectPlugin.Plugin.GetOperations().GetOperationsOfType<T>()
                   select CreatePluginOperationData<T>(project, projectPlugin, pluginOperationMetadata);
        }

        private static PluginOperationData<T> CreatePluginOperationData<T>(Project project, PluginWithConfig projectPlugin,
          PluginOperationMetadata pluginOperationMetadata) where T : IPluginOperation
        {
            return new PluginOperationData<T>(
              $"{projectPlugin.Plugin.GetName()}.{pluginOperationMetadata.Name}",
              () =>
                (T)pluginOperationMetadata.CreateInstance(
                  new PluginConfiguration(project.ProjectName,
                  projectPlugin.Configuration)),
              pluginOperationMetadata.Description, pluginOperationMetadata.AllowPlayerAccess,
              pluginOperationMetadata.FieldMapping);
        }

        private class PluginWithConfig
        {
            public IPlugin Plugin { get; set; }
            public string Configuration { get; set; }
        }

        private IEnumerable<PluginWithConfig> GetProjectInstalledPlugins(Project project)
        {
            return project.ProjectPlugins.Join(Plugins, pp => pp.Name, p => p.GetName(),
              (pp, p) => new PluginWithConfig { Plugin = p, Configuration = pp.Configuration });
        }

        public IEnumerable<HtmlCardPrintResult> PrintForCharacter(PluginOperationData<IPrintCardPluginOperation> pluginInstance, Character c) => pluginInstance.CreatePluginInstance().PrintForCharacter(c.ToPluginModel());

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
                    var projectPlugin = project.ProjectPlugins.SingleOrDefault(pp => pp.Name == pluginName);
                    return new ProjectPluginInfo(
                  pluginName,
                  plugin.GetOperations().GetOperationsOfType<IStaticPagePluginOperation>().Select(o => pluginName + "." + o.Name).ToList(),
                  plugin.GetDescripton(),
                  plugin.GetOperations().Select(o => o.FieldMapping).ToList(),
                  projectPlugin?.PluginFieldMappings.ToList(),
                  projectPlugin?.ProjectPluginId
                  );
                }
              )
              .ToList();
        }

        public async Task<ProjectPluginConfiguraton> GetConfiguration(int projectid, string plugin)
        {
            var project = await ProjectRepository.GetProjectWithDetailsAsync(projectid);
            var pluginInstance = GetProjectInstalledPlugins(project).SingleOrDefault(p => p.Plugin.GetName() == plugin);
            if (pluginInstance == null)
            {
                return null;
            }

            return new ProjectPluginConfiguraton(pluginInstance.Plugin.GetName(),
              new MarkdownString($"```\n{pluginInstance.Configuration}\n```"));
        }

        public string GenerateDefaultCharacterFieldValue(Character character, ProjectField field)
        {
            var operations = GetProjectOperations<IGenerateFieldOperation>(field.Project).HasMapping(field);
            return operations.Select(o => o.CreatePluginInstance()
                .GenerateFieldValue(character.ToPluginModel(), new CharacterFieldInfo(field.ProjectFieldId, null, field.FieldName, null)))
              .FirstOrDefault(newValue => newValue != null);
        }
    }

    public static class OperationsFilters
    {
        public static IEnumerable<PluginOperationData<T>> HasMapping<T>(
          this IEnumerable<PluginOperationData<T>> operations, ProjectField field)
        where T : IPluginOperation, IFieldOperation
        {
            return operations
              .Where(o => field.Mappings.Any(m => m.MappingName == o.FieldMapping &&
                                                  m.PluginFieldMappingType ==
                                                  PluginFieldMappingType.GenerateDefault));
        }

        public static IEnumerable<PluginOperationMetadata> GetOperationsOfType<T>(
          this IEnumerable<PluginOperationMetadata> operations)
          where T : IPluginOperation
          => operations.Where(o => typeof(T).IsAssignableFrom(o.Operation));
    }
}
