using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Parsing;

namespace JoinRpg.Portal.Infrastructure.Logging.Formatting;

internal class CustomJsonFormatter : ElasticsearchJsonFormatter
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    private readonly HashSet<string> _topLevelPropertiesNames;

    public CustomJsonFormatter(HashSet<string> topLevelPropertiesNames) : base(renderMessage: true, renderMessageTemplate: false)
    {
        _topLevelPropertiesNames = topLevelPropertiesNames;
    }

    protected override void WriteTimestamp(DateTimeOffset timestamp, ref string delim, TextWriter output)
    {
        WriteJsonProperty("@timestamp", timestamp.ToUniversalTime().ToString(DateTimeFormat), ref delim, output);
    }

    protected override void WriteLevel(LogEventLevel level, ref string delim, TextWriter output)
    {
        WriteJsonProperty("Level", LevelConvert.ToExtensionsLevel(level), ref delim, output);
    }

    protected override void WriteProperties(IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
    {
        var precedingDelimiter = ",";
        foreach (var kv in properties.Where(pair => _topLevelPropertiesNames.Contains(pair.Key)))
        {
            WriteJsonProperty(kv.Key, kv.Value, ref precedingDelimiter, output);
        }

        output.Write(",\"{0}\":{{", "fields");

        precedingDelimiter = "";
        foreach (var kv in properties.Where(pair => !_topLevelPropertiesNames.Contains(pair.Key)))
        {
            WriteJsonProperty(kv.Key, kv.Value, ref precedingDelimiter, output);
        }

        output.Write("}");
    }

    protected override void WriteRenderings(IGrouping<string, PropertyToken>[] tokensWithFormat, IReadOnlyDictionary<string, LogEventPropertyValue> properties,
        TextWriter output)
    {
        // Don't write information about template renderings
    }
}
