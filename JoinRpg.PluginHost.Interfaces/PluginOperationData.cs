using System;
using JetBrains.Annotations;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.PluginHost.Interfaces
{
  public class PluginOperationData<T> where T : IPluginOperation
  {
    public PluginOperationData(string operationName, Func<T> createPluginInstance)
    {
      OperationName = operationName;
      CreatePluginInstance = createPluginInstance;
    }

    [PublicAPI]
    public string OperationName { get; }

    [PublicAPI]
    public Func<T> CreatePluginInstance { get; }
  }
}
