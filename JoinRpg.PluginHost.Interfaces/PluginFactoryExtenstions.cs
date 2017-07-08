using System;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.PluginHost.Interfaces
{
  public static class PluginFactoryExtenstions
  {
    [CanBeNull]
    public static PluginOperationData<T> GetOperationInstance<T>([NotNull] this IPluginFactory self, Project project, string plugin)
      where T : IPluginOperation
    {
      if (self == null) throw new ArgumentNullException(nameof(self));
      return self.GetProjectOperations<T>(project).SingleOrDefault(p => p.OperationName == plugin);
    }
  }
}