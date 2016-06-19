using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.PluginHost.Interfaces
{
  public interface IPluginFactory
  {
    Task<IEnumerable<PluginOperationData<T>>> GetPossibleOperations<T>(int projectId) where T:IPluginOperation;

    [ItemCanBeNull]
    Task<PluginOperationData<T>> GetOperationInstance<T>(int projectid, string plugin) where T : IPluginOperation;
    IEnumerable<HtmlCardPrintResult> PrintForCharacter(PluginOperationData<IPrintCardPluginOperation> pluginInstance, Character c);
    MarkdownString ShowPluginConfiguration(PluginOperationData<IShowConfigurationPluginOperation> pluginInstance);
  }
}