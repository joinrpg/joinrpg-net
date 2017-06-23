using System.Collections.Generic;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public interface IPlugin
  {
    [NotNull, ItemNotNull]
    IEnumerable<PluginOperationMetadata> GetOperations();

    [NotNull]
    T GetOperationInstance<T>(string operationName, [NotNull] PluginConfiguration pluginConfiguration)
      where T : IPluginOperation;

    [NotNull]
    string GetName();
  }
}
