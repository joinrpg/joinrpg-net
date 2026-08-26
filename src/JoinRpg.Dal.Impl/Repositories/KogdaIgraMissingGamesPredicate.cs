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
    ///    все привязанные КИ-игры уже завершились (End &lt; now, End не пустой) И проект продолжает
    ///    обновляться заметно позже завершения последней игры (&gt;60 дней после её End) —
    ///    просто активность сразу после конца игры (пост-обработка) сигналом не считается.
    /// </summary>
    public static Expression<Func<Project, DateTime, bool>> GetPredicate(DateTime now)
    {
        var gap = TimeSpan.FromDays(60);
        return (project, lastUpdated) =>
            project.Active &&
            !project.Details.DisableKogdaIgraMapping &&
            !project.KogdaIgraGames.Any(g => g.Active && (g.End == null || g.End >= now)) &&
            (!project.KogdaIgraGames.Any(g => g.Active)
                || lastUpdated > project.KogdaIgraGames.Where(g => g.Active).Max(g => g.End!.Value).Add(gap));
    }
}
