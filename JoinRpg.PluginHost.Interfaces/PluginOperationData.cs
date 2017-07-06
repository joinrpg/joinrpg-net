using System;
using JetBrains.Annotations;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.PluginHost.Interfaces
{
  public class PluginOperationData<T> where T : IPluginOperation
  {
    public PluginOperationData(
      [NotNull] string operationName, 
      [NotNull] Func<T> createPluginInstance, 
      [NotNull] string description, 
      bool allowPlayerAccess, 
      [CanBeNull] string fieldMapping)
    {
      if (operationName == null) throw new ArgumentNullException(nameof(operationName));
      if (createPluginInstance == null) throw new ArgumentNullException(nameof(createPluginInstance));
      if (description == null) throw new ArgumentNullException(nameof(description));

      OperationName = operationName;
      CreatePluginInstance = createPluginInstance;
      Description = description;
      AllowPlayerAccess = allowPlayerAccess;
      FieldMapping = fieldMapping;
    }

    [PublicAPI, NotNull]
    public string OperationName { get; }

    [PublicAPI, NotNull]
    public Func<T> CreatePluginInstance { get; }

    [PublicAPI, NotNull]
    public string Description { get; } 

    [PublicAPI]
    public bool AllowPlayerAccess{ get; }

    [PublicAPI, CanBeNull]
    public string FieldMapping { get; }
  }
}
