using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.PluginHost.Interfaces
{
  public interface IPluginFactory
  {
    Task<IEnumerable<PluginOperationData<T>>> GetPossibleOperations<T>(int projectId) where T:IPluginOperation;
    Task<T> GetOperationInstance<T>(int projectid, string plugin) where T : class, IPluginOperation;
  }
}