using System;
using JetBrains.Annotations;
using JoinRpg.Experimental.Plugin.Interfaces;
using Newtonsoft.Json;

namespace JoinRpg.PluginHost.Impl
{
    public class PluginConfiguration : IPluginConfiguration
    {
        public PluginConfiguration([NotNull] string projectName, [NotNull] string configurationString)
        {
            if (projectName == null) throw new ArgumentNullException(nameof(projectName));
            if (configurationString == null) throw new ArgumentNullException(nameof(configurationString));
            ProjectName = projectName;
            ConfigurationString = configurationString;
        }

        private string ConfigurationString { get; }

        [NotNull]
        public T GetConfiguration<T>() => JsonConvert.DeserializeObject<T>(ConfigurationString);

        [NotNull]
        public string ProjectName { get; }
    }
}
