using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.WebInfrastructure.Logging;

internal class SerilogOptions
{
    public Dictionary<string, LogLevel> LogLevel { get; set; } = new();
    public bool Structured { get; set; } = false;
    public bool SelfLogEnabled { get; set; } = false;
}
