using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.PluginHost.Interfaces
{
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

        [CanBeNull]
        string GenerateDefaultCharacterFieldValue([NotNull] Character character, [NotNull] ProjectField field);
    }
}
