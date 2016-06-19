using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.PluginHost.Impl
{
  public class PluginFactoryImpl : IPluginFactory
  {
    private IProjectRepository ProjectRepository { get; }
    private IPluginResolver PluginResolver { get; }
    public PluginFactoryImpl(IProjectRepository projectRepository, IPluginResolver pluginResolver)
    {
      ProjectRepository = projectRepository;
      PluginResolver = pluginResolver;
    }


    public async Task<IEnumerable<PluginOperationData<T>>> GetPossibleOperations<T>(int projectId) where T : IPluginOperation
    {
      var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
      return ReturnPlugins<T>(project);
    }

    private IEnumerable<PluginOperationData<T>> ReturnPlugins<T>(Project project) where T : IPluginOperation
    {
      if (!project.ProjectPlugins.Any())
      {
        yield break;
      }
      foreach (
        var projectPlugin in
          project.ProjectPlugins.Join(PluginResolver.Resolve(), pp => pp.Name, p => p.GetName(),
            (pp, p) => new
            {
              Plugin = p,
              pp.Configuration
            }))
      {
        foreach (
          var pluginOperationMetadata in
            projectPlugin.Plugin.GetOperations().Where(o => typeof(T).IsAssignableFrom(o.Operation)))
        {
          yield return
            new PluginOperationData<T>(
              $"{projectPlugin.Plugin.GetName()}.{pluginOperationMetadata.Name}",
              () =>
                projectPlugin.Plugin.GetOperationInstance<T>(project.ProjectId, pluginOperationMetadata.Name,
                  projectPlugin.Configuration), pluginOperationMetadata.Description, pluginOperationMetadata.AllowPlayerAccess);
        }

      }
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

    public MarkdownString ShowPluginConfiguration(PluginOperationData<IShowConfigurationPluginOperation> pluginInstance, Project project)
    {
      return
        pluginInstance.CreatePluginInstance()
          .ShowPluginConfiguration(
            project.CharacterGroups.Select(g => new CharacterGroupInfo(g.CharacterGroupId, g.CharacterGroupName)),
            project.ProjectFields.Select(
              f =>
                new ProjectFieldInfo(f.ProjectFieldId, f.FieldName,
                  f.DropdownValues.ToDictionary(fv => fv.ProjectFieldDropdownValueId, fv => fv.Label))));
    }

    private static CharacterInfo PrepareCharacterForPlugin(Character character)
    {
      return new CharacterInfo(character.CharacterName,
        character.GetFields()
          .Select(f => new CharacterFieldInfo(f.Field.ProjectFieldId, f.Value, f.Field.FieldName, f.DisplayString)),
        character.CharacterId,
        character.GetParentGroupsToTop().Distinct()
          .Where(g => g.IsActive && !g.IsSpecial && !g.IsRoot)
          .Select(g => new CharacterGroupInfo(g.CharacterGroupId, g.CharacterGroupName)));
    }
  }
}