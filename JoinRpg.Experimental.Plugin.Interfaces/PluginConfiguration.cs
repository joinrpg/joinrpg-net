using System;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
    public interface IPluginConfiguration
    {
        T GetConfiguration<T>();
        string ProjectName { get; }
    }

}
