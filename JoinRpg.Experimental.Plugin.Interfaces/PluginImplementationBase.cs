using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public abstract class PluginImplementationBase : IPlugin
  {
    private readonly IList<PluginOperationMetadata> _operations = new List<PluginOperationMetadata>();

    protected void Register<T>([NotNull] string name, [NotNull] string description = "", bool allowPlayerAccess = false) where T: IPluginOperation, new()
    {
      if (name == null) throw new ArgumentNullException(nameof(name));
      if (description == null) throw new ArgumentNullException(nameof(description));

      _operations.Add(new PluginOperationMetadata(name, typeof(T), description, allowPlayerAccess, () => new T()));
    }

    public IEnumerable<PluginOperationMetadata> GetOperations() => _operations;

    public abstract string GetName();
    public abstract string GetDescripton();
  }
}