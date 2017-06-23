using System;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public class PluginConfiguration
  {
    public PluginConfiguration([NotNull] string projectName, [NotNull] string configurationString)
    {
      if (projectName == null) throw new ArgumentNullException(nameof(projectName));
      if (configurationString == null) throw new ArgumentNullException(nameof(configurationString));
      ProjectName = projectName;
      ConfigurationString = configurationString;
    }

    [NotNull]
    public string ConfigurationString { get; }

    [NotNull]
    public string ProjectName { get; }
  }
}
