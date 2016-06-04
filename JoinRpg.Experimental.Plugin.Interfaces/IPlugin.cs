using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public interface IPlugin
  {
    IEnumerable<PluginOperationMetadata> GetOperations();

    T GetOperationInstance<T>(int projectId, string operationName, string pluginConfiguration)
      where T : IPluginOperation;

    string GetName();
  }

  public abstract class PluginImplementationBase : IPlugin
  {
    private readonly IDictionary<string, OperationLink> _operations = new Dictionary<string, OperationLink>();

    private class OperationLink
    {
      public Type Operation { get; }
      public Func<string, IPluginOperation> Implementer { get; }

      public OperationLink(Type operation, Func<string, IPluginOperation> implementer)
      {
        Operation = operation;
        Implementer = implementer;
      }
    }

    protected void Register<T>(string name, Func<string, T> implementer) where T: IPluginOperation
    {
      _operations.Add(name, new OperationLink(typeof(T), c => implementer(c)));
    }

    public IEnumerable<PluginOperationMetadata> GetOperations()
    {
      return
        _operations.Select(
          o =>
            new PluginOperationMetadata(o.Key,
              o.Value.Operation.GetInterfaces().Single(i => typeof(IPluginOperation).IsAssignableFrom(i) && i != typeof(IPluginOperation))));
    }

    public T GetOperationInstance<T>(int projectId, string operationName, string pluginConfiguration)
      where T : IPluginOperation
    {
      return (T)_operations[operationName].Implementer(pluginConfiguration);
    }

    public abstract string GetName();
  }
}
