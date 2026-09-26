using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Зафиксированное число ленивых загрузок по маршрутам. Тест падает только при росте (#4914).
/// </summary>
/// <remarks>
/// По умолчанию требуется ноль. В файле перечислены только маршруты, где долг ещё есть (#4670),
/// и задача — гнать этот список вниз, пока он не опустеет.
///
/// Чтобы пересобрать снапшот (в том числе опустить подросшие вниз), прогнать тесты с
/// <c>JOINRPG_LAZYLOAD_BASELINE=update</c> — файл рядом с этим классом перезапишется
/// измеренными значениями, и правки надо закоммитить.
/// </remarks>
public sealed class LazyLoadBaseline
{
    private const string FileName = "lazy-loads-baseline.json";
    private const string UpdateEnvironmentVariable = "JOINRPG_LAZYLOAD_BASELINE";

    public static LazyLoadBaseline Instance { get; } = Load();

    private readonly IReadOnlyDictionary<string, int> baseline;
    private readonly ConcurrentDictionary<string, int> measured = new(StringComparer.Ordinal);
    private readonly bool updateMode;

    internal LazyLoadBaseline(IReadOnlyDictionary<string, int> baseline, bool updateMode)
    {
        this.baseline = baseline;
        this.updateMode = updateMode;
    }

    /// <summary>Сверяет замер со снапшотом; в режиме обновления просто копит измеренное.</summary>
    public void Check(LazyLoadObservation observation)
    {
        if (updateMode)
        {
            _ = measured.AddOrUpdate(
                observation.Route,
                observation.LazyLoads,
                (_, previous) => Math.Max(previous, observation.LazyLoads));
            return;
        }

        // Маршрута нет в снапшоте — значит ленивых загрузок на нём быть не должно.
        var allowed = baseline.GetValueOrDefault(observation.Route);
        if (observation.LazyLoads <= allowed)
        {
            return;
        }

        throw new LazyLoadBaselineException(
            $"Маршрут '{observation.Route}' сделал {observation.LazyLoads} ленивых загрузок вместо {allowed} "
            + $"({(baseline.ContainsKey(observation.Route) ? "зафиксировано в снапшоте" : "маршрута нет в снапшоте, значит ленивых загрузок быть не должно")}). "
            + "Скорее всего в запрос вернулся N+1 — добавьте Include или проекцию (см. #4670). "
            + $"Если рост осознанный, пересоберите снапшот: {UpdateEnvironmentVariable}=update dotnet test src/JoinRpg.IntegrationTest");
    }

    private static LazyLoadBaseline Load()
    {
        var updateMode = string.Equals(
            Environment.GetEnvironmentVariable(UpdateEnvironmentVariable),
            "update",
            StringComparison.OrdinalIgnoreCase);

        var path = Path.Combine(AppContext.BaseDirectory, FileName);
        var loaded = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(path)) ?? []
            : [];

        var instance = new LazyLoadBaseline(loaded, updateMode);

        if (updateMode)
        {
            // Снапшот пишется один раз в конце прогона: до этого момента неизвестно,
            // какие маршруты дёрнулись и каким оказался максимум по каждому.
            AppDomain.CurrentDomain.ProcessExit += (_, _) => instance.Save();
        }

        return instance;
    }

    private void Save([CallerFilePath] string callerFilePath = "")
    {
        // Пишем именно в исходники, а не в bin: снапшот надо закоммитить.
        var sourcePath = Path.Combine(Path.GetDirectoryName(callerFilePath)!, FileName);

        // Маршруты, не затронутые прогоном, сохраняем как были — иначе выборочный прогон
        // тестов стёр бы чужие замеры.
        var result = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var (route, count) in baseline)
        {
            result[route] = count;
        }
        foreach (var (route, count) in measured)
        {
            // Ноль — это значение по умолчанию, в файле он не нужен: снапшот содержит
            // только маршруты, на которых долг ещё есть.
            if (count == 0)
            {
                _ = result.Remove(route);
            }
            else
            {
                result[route] = count;
            }
        }

        File.WriteAllText(
            sourcePath,
            JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        Console.WriteLine($"[LazyLoadBaseline] Снапшот обновлён: {sourcePath}");
    }
}

/// <summary>Маршрут стал делать больше ленивых загрузок, чем зафиксировано в снапшоте.</summary>
public class LazyLoadBaselineException(string message) : Exception(message);
