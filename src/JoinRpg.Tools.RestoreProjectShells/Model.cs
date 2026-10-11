namespace JoinRpg.Tools.RestoreProjectShells;

/// <summary>
/// Состояние основной БД (MSSQL), нужное для решения по строкам списка.
/// </summary>
/// <param name="ExistingProjects">Проекты из списка, которые уже есть в MSSQL: id → название.</param>
/// <param name="ExistingUserIds">Создатели из списка, которые есть в Users.</param>
/// <param name="ShiftedProjectsSeed">
/// Счётчик Projects, записанный reseed-identities.sql в drp.ReseedLog. null — счётчик не сдвигался.
/// </param>
internal record MainDatabaseState(
    IReadOnlyDictionary<int, string> ExistingProjects,
    IReadOnlySet<int> ExistingUserIds,
    long? ShiftedProjectsSeed);

internal enum ShellDecisionKind
{
    /// <summary>Создать оболочку.</summary>
    Create,
    /// <summary>В колонке «Создавать» — «нет».</summary>
    NotRequested,
    /// <summary>Тот же id встречается в списке больше одного раза.</summary>
    DuplicateId,
    /// <summary>Id не выше максимума бэкапа — такой проект в бэкапе был.</summary>
    NotAboveBackupMax,
    /// <summary>Проект с таким id уже есть: создан прошлым запуском.</summary>
    AlreadyExists,
    /// <summary>Нет названия.</summary>
    NoName,
    /// <summary>Создатель не указан.</summary>
    NoCreator,
    /// <summary>Создателя нет в Users.</summary>
    CreatorNotFound,
    /// <summary>Счётчик Projects не сдвигался reseed-identities.sql.</summary>
    CounterNotShifted,
    /// <summary>Id не ниже сдвинутого счётчика: подмена не нужна и опасна.</summary>
    NotBelowCounter,
}

/// <summary>
/// Решение по одной строке списка.
/// </summary>
/// <param name="MssqlName">Название проекта с этим id, который уже есть в MSSQL, — для сверки.</param>
internal record ShellDecision(int ProjectId, ShellDecisionKind Kind, string? Name, int? CreatorUserId, string? MssqlName = null)
{
    public string Reason => Kind switch
    {
        ShellDecisionKind.Create => "будет создан",
        ShellDecisionKind.NotRequested => "пропущен при ручной проверке",
        ShellDecisionKind.DuplicateId => "id встречается в списке несколько раз",
        ShellDecisionKind.NotAboveBackupMax => "id не выше максимума бэкапа",
        ShellDecisionKind.AlreadyExists => "уже создан",
        ShellDecisionKind.NoName => "нет названия",
        ShellDecisionKind.NoCreator => "не указан создатель",
        ShellDecisionKind.CreatorNotFound => "создателя нет в Users",
        ShellDecisionKind.CounterNotShifted => "счётчик Projects не сдвигался reseed-identities.sql (нет записи в drp.ReseedLog)",
        ShellDecisionKind.NotBelowCounter => "id не ниже сдвинутого счётчика Projects",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
}
