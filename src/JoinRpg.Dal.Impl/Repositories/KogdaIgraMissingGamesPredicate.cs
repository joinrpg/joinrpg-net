using JoinRpg.DataModel.Projects;

namespace JoinRpg.Dal.Impl.Repositories;

internal static class KogdaIgraMissingGamesPredicate
{
    /// <summary>
    /// Определяет, нуждается ли проект в привязке к КогдаИгра.
    /// Проект попадает в выборку если:
    /// 1. Нет активных КИ-привязок вообще, ИЛИ
    /// 2. Все привязанные КИ-игры уже завершились (End &lt; now) И проект недавно обновлялся (&lt;60 дней)
    /// </summary>
    public static bool NeedsBinding(
        IEnumerable<KogdaIgraGame> games,
        DateTime lastUpdated,
        DateTime now)
    {
        var lastUpdateMax = now.AddDays(-60);
        var hasActiveOrFutureGame = games.Any(g => g.Active && g.End >= now);
        var hasAnyGame = games.Any(g => g.Active);
        return !hasActiveOrFutureGame && (!hasAnyGame || lastUpdated > lastUpdateMax);
    }
}
