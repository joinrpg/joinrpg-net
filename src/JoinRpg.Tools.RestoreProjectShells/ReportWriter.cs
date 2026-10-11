using System.Globalization;
using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreProjectShells;

/// <summary>
/// CSV-отчёт с решением по каждой строке списка. Хранить только локально.
/// </summary>
internal static class ReportWriter
{
    public static readonly IReadOnlyList<string> Header =
        ["ProjectId", "Решение", "Причина", "Результат", "Название", "СоздательId", "НазваниеВMssql"];

    /// <param name="results">Что вышло при --apply: id проекта → результат. Пусто в dry-run.</param>
    public static IEnumerable<IReadOnlyList<string?>> Rows(IEnumerable<ShellDecision> decisions, IReadOnlyDictionary<int, string> results)
    {
        yield return Header;
        foreach (var d in decisions)
        {
            yield return
            [
                d.ProjectId.ToString(CultureInfo.InvariantCulture),
                d.Kind.ToString(),
                d.Reason,
                results.GetValueOrDefault(d.ProjectId),
                DrpCsv.UserText(d.Name),
                d.CreatorUserId?.ToString(CultureInfo.InvariantCulture),
                DrpCsv.UserText(d.MssqlName),
            ];
        }
    }

    public static Task Write(string path, IEnumerable<ShellDecision> decisions, IReadOnlyDictionary<int, string> results, CancellationToken ct)
        => DrpCsv.WriteFile(path, Rows(decisions, results), ct);
}
