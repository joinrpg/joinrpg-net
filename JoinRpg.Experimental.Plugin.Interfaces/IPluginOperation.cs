using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public interface IPluginOperation
  {
    void SetConfiguration([NotNull] IPluginConfiguration pluginConfiguration);
  }
}
