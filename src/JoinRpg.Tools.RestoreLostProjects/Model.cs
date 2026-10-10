namespace JoinRpg.Tools.RestoreLostProjects;

/// <summary>
/// Строка базы уведомлений, которая относится к одному из интересных проектов.
/// Тело целиком не читаем — из него нужны только признаки, посчитанные в SQL.
/// </summary>
/// <param name="EntityReference">Значение EntityReference как есть.</param>
/// <param name="MasterInitiated">Вторая строка тела «Заявка … мастером …» — инициатор действует как мастер.</param>
/// <param name="NewClaim">В теле «**Новая заявка**» — получатели такого уведомления мастера.</param>
/// <param name="AdminBody">Тело, только у уведомления админам о новом проекте.</param>
internal record ProjectNotification(
    string EntityReference,
    string Header,
    int InitiatorUserId,
    int RecipientUserId,
    DateTimeOffset CreatedAt,
    bool MasterInitiated,
    bool NewClaim,
    string? AdminBody);

/// <summary>
/// Запись из сохранённых логов k8s. Все поля необязательны: формат выгрузки мог различаться.
/// </summary>
internal record LogEntry(
    int? ProjectId,
    string? LoggedUser,
    string? RequestPath,
    string? ActionName,
    DateTimeOffset? Timestamp);

/// <summary>
/// Пользователь, который уже есть в основной БД (MSSQL).
/// </summary>
internal record ExistingUser(int UserId, string? Email);

/// <summary>
/// Откуда взят вариант названия проекта.
/// </summary>
internal enum HeaderKind
{
    /// <summary>«Новый проект «…» — статус КогдаИгры» — уведомление админам о создании проекта.</summary>
    AdminNewProject,
    /// <summary>«{Проект}: {персонаж}, игрок {…}».</summary>
    Claim,
    /// <summary>«{Проект}: тема на форуме …».</summary>
    Forum,
    /// <summary>«{Проект}: опубликована вводная».</summary>
    Plot,
    /// <summary>«{Проект}: комната …».</summary>
    Room,
    /// <summary>«{Проект}: приглашения к проживанию».</summary>
    Invites,
    /// <summary>«{Проект}: проект закрыт» и «{Проект}: проект будет закрыт из-за неактивности».</summary>
    ProjectClosed,
    /// <summary>«{Проект}: {тема}» — массовая рассылка, только с 2026-09-28.</summary>
    MassMail,
}

/// <summary>
/// Вариант названия проекта и сколько уведомлений каждого вида его подтверждают.
/// </summary>
internal record NameVariant(string Name, IReadOnlyDictionary<HeaderKind, int> NotificationsByKind)
{
    public int Notifications => NotificationsByKind.Values.Sum();
}

/// <summary>
/// Пользователь, который по уведомлениям проекта выступает мастером: сколько таких свидетельств
/// и когда первое.
/// </summary>
internal record CreatorCandidate(int UserId, int Evidence, DateTimeOffset FirstSeen);

internal enum CreatorMethod
{
    /// <summary>Создатель не найден.</summary>
    NotFound,
    /// <summary>Инициатор уведомления админам о новом проекте.</summary>
    Exact,
    /// <summary>Самый ранний мастер по уведомлениям проекта.</summary>
    Presumed,
}

/// <summary>
/// Привязка к КогдаИгре из тела уведомления админам о новом проекте.
/// </summary>
internal record KogdaIgraLink(bool OnKogdaIgra, int? GameId)
{
    public override string ToString() => this switch
    {
        { OnKogdaIgra: false } => "игры нет на КогдаИгре",
        { GameId: int id } => $"/game/{id}/",
        _ => "игра есть на КогдаИгре, id не указан",
    };
}

[Flags]
internal enum ProjectFlags
{
    None = 0,
    /// <summary>Несколько вариантов названия, ни один не объясняет все заголовки.</summary>
    NameConflict = 1,
    /// <summary>Название не найдено.</summary>
    NameNotFound = 2,
    /// <summary>Создатель не найден.</summary>
    CreatorNotFound = 4,
    /// <summary>Создателя нет в MSSQL.</summary>
    CreatorMissingInMssql = 8,
    /// <summary>Id не больше майского максимума, но проекта в MSSQL нет — противоречие.</summary>
    IdNotAboveBackupMax = 16,
    /// <summary>Проект с этим id уже есть в MSSQL (например, оболочка из прошлого прогона).</summary>
    AlreadyInMssql = 32,
}

/// <summary>
/// Строка отчёта — один проект.
/// </summary>
internal record LostProjectReport(
    int ProjectId,
    string? Name,
    IReadOnlyList<NameVariant> NameVariants,
    int? CreatorUserId,
    string? CreatorEmail,
    CreatorMethod CreatorMethod,
    IReadOnlyList<CreatorCandidate> OtherCreatorCandidates,
    DateTimeOffset? FirstActivity,
    DateTimeOffset? LastActivity,
    int Notifications,
    int LogRequests,
    IReadOnlyList<string> LogUsers,
    bool? CreatorInLogs,
    KogdaIgraLink? KogdaIgra,
    string? MssqlName,
    ProjectFlags Flags);
