using System.Diagnostics;

namespace JoinRpg.Integrations.KogdaIgra;

public static class KogdaIgraSyncServiceActivity
{
    public const string ActivitySourceName = nameof(KogdaIgraSyncService);
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
