namespace JoinRpg.Tools.RestoreLostUsers;

/// <summary>
/// Где и когда пользователь встречается в базе уведомлений — как получатель или как инициатор.
/// </summary>
internal record NotificationUserActivity(
    int UserId,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    int AsRecipient,
    int AsInitiator);

/// <summary>
/// Значение ChannelSpecificValue (как есть) и когда по нему последний раз слали уведомление.
/// </summary>
internal record ChannelValue(string Value, DateTimeOffset LastSentAt);

/// <summary>
/// Подробности из базы уведомлений по одному пользователю: все адреса почты, куда ему слали уведомления,
/// и последний Telegram (значение ChannelSpecificValue как есть).
/// </summary>
internal record NotificationUserDetails(
    int UserId,
    IReadOnlyCollection<ChannelValue> EmailValues,
    string? LatestTelegramValue);

/// <summary>
/// Пользователь, который уже есть в основной БД (MSSQL).
/// </summary>
internal record ExistingUser(int UserId, string? Email, string? UserName);

internal enum DecisionKind
{
    /// <summary>Создать заготовку пользователя.</summary>
    Create,
    /// <summary>В уведомлениях есть только как инициатор, адреса почты нет.</summary>
    NoEmail,
    /// <summary>Адрес почты из уведомлений не разбирается.</summary>
    InvalidEmail,
    /// <summary>Адрес почты уже занят в MSSQL другим пользователем.</summary>
    EmailTaken,
    /// <summary>Тот же адрес почты у нескольких кандидатов.</summary>
    DuplicateEmail,
    /// <summary>Id не больше максимума из бэкапа, но в MSSQL его нет — противоречие.</summary>
    IdNotAboveBackupMax,
    /// <summary>Некорректный id (0 или отрицательный).</summary>
    InvalidId,
    /// <summary>
    /// Id выше максимума из бэкапа уже есть в MSSQL с тем же адресом почты, что и до аварии:
    /// восстановлен прошлым запуском.
    /// </summary>
    AlreadyExists,
    /// <summary>
    /// Id после бэкапа уже занят в MSSQL пользователем с другим адресом почты (или до аварии адреса
    /// не было вовсе) — скорее всего, новым регистрантом, если сайт открыли до восстановления.
    /// </summary>
    IdTakenByOtherUser,
}

/// <summary>
/// Решение по одному пользователю. AllEmails — все различные адреса почты, куда слали уведомления
/// до аварии, если их больше одного (только для отчёта; для заготовки берётся последний).
/// </summary>
internal record LostUserDecision(
    int UserId,
    DecisionKind Kind,
    string? Email,
    long? TelegramChatId,
    string? TelegramUserName,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    int AsRecipient,
    int AsInitiator,
    int? ConflictingUserId,
    IReadOnlyList<string>? AllEmails = null)
{
    public string Reason => Kind switch
    {
        DecisionKind.Create => "будет создан",
        DecisionKind.NoEmail => "нет адреса почты: в уведомлениях только как инициатор или без почтового канала",
        DecisionKind.InvalidEmail => "адрес почты из уведомлений не разбирается",
        DecisionKind.EmailTaken => "адрес почты уже занят в MSSQL другим пользователем (вероятно, сменил почту после бэкапа)",
        DecisionKind.DuplicateEmail => "тот же адрес почты у нескольких потерянных пользователей",
        DecisionKind.IdNotAboveBackupMax => "id не больше максимума из бэкапа, но в MSSQL его нет — противоречие",
        DecisionKind.InvalidId => "некорректный id",
        DecisionKind.AlreadyExists => "уже есть в MSSQL с тем же адресом почты (восстановлен ранее)",
        DecisionKind.IdTakenByOtherUser => "id после бэкапа занят в MSSQL пользователем с другим адресом почты (или до аварии адреса не было)",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
}
