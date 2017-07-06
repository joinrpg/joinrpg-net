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
    string GetName();

    [NotNull]
    string GetDescripton();
  }
}
