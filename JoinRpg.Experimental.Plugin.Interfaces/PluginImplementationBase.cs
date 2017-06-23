using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public abstract class PluginImplementationBase : IPlugin
  {
    private readonly IDictionary<string, OperationLink> _operations = new Dictionary<string, OperationLink>();

    private class OperationLink
    {
      public Type Operation { get; }
      public Func<PluginConfiguration, IPluginOperation> Implementer { get; }
      public string Description { get; }
      public bool AllowPlayerAccess { get; }

      public OperationLink(Type operation, Func<PluginConfiguration, IPluginOperation> implementer, string description, bool allowPlayerAccess)
      {
        Operation = operation;
        Implementer = implementer;
        Description = description;
        AllowPlayerAccess = allowPlayerAccess;
      }
    }

    protected void Register<T>([NotNull] string name, [NotNull] Func<PluginConfiguration, T> implementer, [NotNull] string description = "", bool allowPlayerAccess = false) where T: IPluginOperation
    {
      if (name == null) throw new ArgumentNullException(nameof(name));
      if (implementer == null) throw new ArgumentNullException(nameof(implementer));
      if (description == null) throw new ArgumentNullException(nameof(description));

      _operations.Add(name, new OperationLink(typeof(T), c => implementer(c), description, allowPlayerAccess));
    }

    protected  void RegisterShowJsonConfiguraton()
    {
      Register("JSON", c => new ShowRawJsonConfiguration(c), "Конфигурация (в исходном виде)");
    }

    public IEnumerable<PluginOperationMetadata> GetOperations()
    {
      return
        _operations.Select(
          o =>
            new PluginOperationMetadata(o.Key,
              o.Value.Operation.GetInterfaces()
                .Single(i => typeof(IPluginOperation).IsAssignableFrom(i) && i != typeof(IPluginOperation)),
              o.Value.Description, o.Value.AllowPlayerAccess));
    }

    public T GetOperationInstance<T>(string operationName, PluginConfiguration pluginConfiguration)
      where T : IPluginOperation
    {
      return (T)_operations[operationName].Implementer(pluginConfiguration);
    }

    public abstract string GetName();
  }
}