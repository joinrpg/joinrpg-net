using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.PluginHost.Interfaces
{
    public sealed class ProjectPluginInfo
    {
        public ProjectPluginInfo(
          [NotNull] string name,
          [NotNull, ItemNotNull] IReadOnlyCollection<string> staticPages,
          [NotNull] string description,
          [NotNull, ItemNotNull] IReadOnlyCollection<string> possibleMappings,
          [CanBeNull, ItemNotNull] IReadOnlyCollection<PluginFieldMapping> activeMappings,
          [CanBeNull] int? projectPluginId)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (staticPages == null) throw new ArgumentNullException(nameof(staticPages));
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (possibleMappings == null) throw new ArgumentNullException(nameof(possibleMappings));
            Name = name;
            Installed = projectPluginId != null;
            StaticPages = staticPages;
            Description = description;
            PossibleMappings = possibleMappings;
            ProjectPluginId = projectPluginId;
            ActiveMappings = activeMappings ?? new PluginFieldMapping[] { };
        }
        [NotNull]
        public string Name { get; }
        public bool Installed { get; }
        [NotNull, ItemNotNull]
        public IReadOnlyCollection<string> StaticPages { get; }
        [NotNull]
        public string Description { get; }
        [NotNull, ItemNotNull]
        public IReadOnlyCollection<string> PossibleMappings { get; }
        [NotNull, ItemNotNull]
        public IReadOnlyCollection<PluginFieldMapping> ActiveMappings { get; }
        [CanBeNull]
        public int? ProjectPluginId { get; }
    }
}
