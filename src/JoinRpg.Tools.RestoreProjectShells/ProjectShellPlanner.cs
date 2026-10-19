using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreProjectShells;

/// <summary>
/// Чистая логика отбора: какие строки проверенного списка превращаются в оболочки проектов.
/// Без обращения к БД — всё нужное передаётся снаружи.
/// </summary>
/// <param name="backupMaxProjectId">Максимальный ProjectId в восстановленном бэкапе.</param>
internal class ProjectShellPlanner(int backupMaxProjectId, MainDatabaseState state)
{
    /// <summary>Id создателей, которых нужно поискать в Users.</summary>
    public static IReadOnlyCollection<int> CreatorIdsToLookUp(IEnumerable<LostProjectsCsvRow> rows)
        => [.. rows.Where(r => r.Create).Select(r => r.CreatorUserId).OfType<int>().Distinct().Order()];

    public IReadOnlyList<ShellDecision> Plan(IReadOnlyCollection<LostProjectsCsvRow> rows)
    {
        var duplicateIds = rows
            .GroupBy(r => r.ProjectId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        return [.. rows.Select(r => Decide(r, duplicateIds)).OrderBy(d => d.ProjectId)];
    }

    private ShellDecision Decide(LostProjectsCsvRow row, HashSet<int> duplicateIds)
    {
        ShellDecision Make(ShellDecisionKind kind)
            => new(row.ProjectId, kind, row.Name, row.CreatorUserId, state.ExistingProjects.GetValueOrDefault(row.ProjectId));

        if (!row.Create)
        {
            return Make(ShellDecisionKind.NotRequested);
        }
        if (duplicateIds.Contains(row.ProjectId))
        {
            return Make(ShellDecisionKind.DuplicateId);
        }
        // Раньше проверки «уже создан»: майский проект с таким id в MSSQL есть, но это не наша оболочка.
        if (row.ProjectId <= backupMaxProjectId)
        {
            return Make(ShellDecisionKind.NotAboveBackupMax);
        }
        // Раньше проверок счётчика: после reseed-identities.sql повторный запуск должен честно сказать «уже создан».
        if (state.ExistingProjects.ContainsKey(row.ProjectId))
        {
            return Make(ShellDecisionKind.AlreadyExists);
        }
        if (row.Name is null)
        {
            return Make(ShellDecisionKind.NoName);
        }
        if (row.CreatorUserId is not int creator)
        {
            return Make(ShellDecisionKind.NoCreator);
        }
        if (!state.ExistingUserIds.Contains(creator))
        {
            return Make(ShellDecisionKind.CreatorNotFound);
        }
        // Сравниваем со счётчиком из журнала, а не с IDENT_CURRENT: инструмент сам опускает счётчик,
        // и после прерванного запуска IDENT_CURRENT равен последнему созданному id.
        if (state.ShiftedProjectsSeed is not long seed)
        {
            return Make(ShellDecisionKind.CounterNotShifted);
        }
        if (row.ProjectId >= seed)
        {
            return Make(ShellDecisionKind.NotBelowCounter);
        }
        return Make(ShellDecisionKind.Create);
    }
}
