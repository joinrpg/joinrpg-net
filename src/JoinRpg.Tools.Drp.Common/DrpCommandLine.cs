namespace JoinRpg.Tools.Drp.Common;

/// <summary>
/// Командная строка DRP-инструмента: флаги режима (например, --apply) и разовые переопределения
/// ключей конфигурации (--lost-at, --report и параметры конкретного инструмента).
/// Свой разбор, а не CommandLine-провайдер конфигурации: тот не умеет флаги без значения
/// и молча принимает опечатки в именах параметров.
/// </summary>
public sealed record DrpCommandLine(IReadOnlyDictionary<string, string?> Overrides, IReadOnlySet<string> Flags)
{
    /// <summary>Переопределения, общие для всех DRP-инструментов.</summary>
    public static readonly IReadOnlyDictionary<string, string> CommonOptions = new Dictionary<string, string>
    {
        ["--lost-at"] = DrpKeys.LostAt,
        ["--report"] = DrpKeys.ReportPath,
    };

    public bool HasFlag(string flag) => Flags.Contains(flag);

    /// <param name="args">Аргументы командной строки.</param>
    /// <param name="toolOptions">Переопределения конкретного инструмента: имя параметра → ключ конфигурации.</param>
    /// <param name="flags">Флаги без значения, которые понимает инструмент.</param>
    /// <returns>Разобранная командная строка или текст ошибки.</returns>
    public static (DrpCommandLine? CommandLine, string? Error) Parse(
        IReadOnlyList<string> args,
        IReadOnlyDictionary<string, string> toolOptions,
        IReadOnlyCollection<string> flags)
    {
        var options = CommonOptions.Concat(toolOptions).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var foundFlags = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (flags.Contains(arg))
            {
                _ = foundFlags.Add(arg);
            }
            else if (options.TryGetValue(arg, out var key))
            {
                if (i + 1 >= args.Count)
                {
                    return (null, $"Не задано значение параметра {arg}");
                }
                overrides[key] = args[++i];
            }
            else
            {
                return (null, $"Неизвестный параметр {arg}");
            }
        }

        return (new DrpCommandLine(overrides, foundFlags), null);
    }
}
