using System;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public class PluginOperationMetadata
  {
    public PluginOperationMetadata([NotNull] string name, [NotNull] Type operation, [NotNull] string description, bool allowPlayerAccess, Func<IPluginOperation> implementer)
    {
      if (name == null) throw new ArgumentNullException(nameof(name));
      if (operation == null) throw new ArgumentNullException(nameof(operation));
      if (description == null) throw new ArgumentNullException(nameof(description));
      Name = name;
      Operation = operation;
      Description = description;
      AllowPlayerAccess = allowPlayerAccess;
      Implementer = implementer;
    }

    [NotNull]
    public string Name { get; }

    [NotNull]
    public Type Operation { get; }

    [NotNull]
    public string Description { get; }

    public bool AllowPlayerAccess { get; }

    public IPluginOperation CreateInstance(IPluginConfiguration pluginConfiguration)
    {
      var pluginOperation = Implementer();
      pluginOperation.SetConfiguration(pluginConfiguration);
      return pluginOperation;
    }

    private Func<IPluginOperation> Implementer { get; }
  }
}