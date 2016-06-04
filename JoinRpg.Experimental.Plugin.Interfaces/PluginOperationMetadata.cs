using System;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public class PluginOperationMetadata
  {
    public PluginOperationMetadata(string name, Type operation)
    {
      Name = name;
      Operation = operation;
    }

    public string Name { get; }
    public Type Operation { get; }
  }
}