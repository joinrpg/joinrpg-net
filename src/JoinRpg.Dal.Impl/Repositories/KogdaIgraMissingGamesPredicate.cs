namespace JoinRpg.Dal.Impl.Repositories;

internal static class KogdaIgraMissingGamesPredicate
{
    /// <summary>
    /// Возвращает Expression-предикат для фильтрации проектов, нуждающихся в привязке к КогдаИгра.
    /// Используется с LinqKit (.AsExpandable() + .Invoke()) для трансляции запроса в SQL.
    /// См. docs/linq-queries.md
    ///
    /// Проект попадает в выборку если:
    /// 1. Проект активен и не отключена привязка к КогдаИгра
    /// 2. Нет активных КИ-привязок вообще, ИЛИ
    ///    все привязанные КИ-игры уже завершились (End &lt; now) И проект недавно обновлялся (&lt;60 дней)
    /// </summary>
    public static Expression<Func<Project, DateTime, bool>> GetPredicate(DateTime now)
    {
        var lastUpdateMax = now.AddDays(-60);
        return (project, lastUpdated) =>
            project.Active &&
            !project.Details.DisableKogdaIgraMapping &&
            !project.KogdaIgraGames.Any(g => g.Active && g.End >= now) &&
            (!project.KogdaIgraGames.Any(g => g.Active) || lastUpdated > lastUpdateMax);
    }
}
