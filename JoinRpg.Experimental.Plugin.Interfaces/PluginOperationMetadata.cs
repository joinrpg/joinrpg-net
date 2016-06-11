﻿using System;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public class PluginOperationMetadata
  {
    public PluginOperationMetadata([NotNull] string name, [NotNull] Type operation, [NotNull] string description, bool allowPlayerAccess)
    {
      if (name == null) throw new ArgumentNullException(nameof(name));
      if (operation == null) throw new ArgumentNullException(nameof(operation));
      if (description == null) throw new ArgumentNullException(nameof(description));
      Name = name;
      Operation = operation;
      Description = description;
      AllowPlayerAccess = allowPlayerAccess;
    }

    [NotNull]
    public string Name { get; }

    [NotNull]
    public Type Operation { get; }

    [NotNull]
    public string Description { get; }

    public bool AllowPlayerAccess { get; }
  }
}