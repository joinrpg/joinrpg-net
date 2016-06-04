using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public interface IPlugin
  {
    [NotNull, ItemNotNull]
    IEnumerable<PluginOperationMetadata> GetOperations();

    [NotNull]
    T GetOperationInstance<T>(int projectId, string operationName, string pluginConfiguration)
      where T : IPluginOperation;

    [NotNull]
    string GetName();
  }

  public abstract class PluginImplementationBase : IPlugin
  {
    private readonly IDictionary<string, OperationLink> _operations = new Dictionary<string, OperationLink>();

    private class OperationLink
    {
      public Type Operation { get; }
      public Func<string, IPluginOperation> Implementer { get; }
      public string Description { get; }

      public OperationLink(Type operation, Func<string, IPluginOperation> implementer, string description)
      {
        Operation = operation;
        Implementer = implementer;
        Description = description;
      }
    }

    protected void Register<T>([NotNull] string name, [NotNull] Func<string, T> implementer, [NotNull] string description = "") where T: IPluginOperation
    {
      if (name == null) throw new ArgumentNullException(nameof(name));
      if (implementer == null) throw new ArgumentNullException(nameof(implementer));
      if (description == null) throw new ArgumentNullException(nameof(description));

      _operations.Add(name, new OperationLink(typeof(T), c => implementer(c), description));
    }

    public IEnumerable<PluginOperationMetadata> GetOperations()
    {
      return
        _operations.Select(
          o =>
            new PluginOperationMetadata(o.Key,
              o.Value.Operation.GetInterfaces()
                .Single(i => typeof(IPluginOperation).IsAssignableFrom(i) && i != typeof(IPluginOperation)),
              o.Value.Description));
    }

    public T GetOperationInstance<T>(int projectId, string operationName, string pluginConfiguration)
      where T : IPluginOperation
    {
      return (T)_operations[operationName].Implementer(pluginConfiguration);
    }

    public abstract string GetName();
  }
}
