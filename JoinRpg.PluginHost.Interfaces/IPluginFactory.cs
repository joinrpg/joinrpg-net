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
    Task<PluginOperationData<IPrintCardPluginOperation>> GetOperationInstance(int projectid, string plugin);
    IEnumerable<HtmlCardPrintResult> PrintForCharacter(PluginOperationData<IPrintCardPluginOperation> pluginInstance, Character c);
  }
}