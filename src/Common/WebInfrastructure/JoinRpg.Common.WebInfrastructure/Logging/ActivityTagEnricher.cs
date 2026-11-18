using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace JoinRpg.Common.WebInfrastructure.Logging;

internal class ActivityTagsEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;

        if (activity is null)
        {
            return;
        }

        foreach (var item in activity.Tags)
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty(item.Key, new ScalarValue(item.Value)));
        }
    }
}
