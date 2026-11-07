namespace JoinRpg.Services.Interfaces.Notification;

/// <summary>
/// Уведомления мастерам в проекте (вне привязок к конкретной заявке)
/// </summary>
public class ProjectEmailModelBase
{
    public required ProjectIdentification ProjectId { get; set; }
}

public class ProjectClosedMail : ProjectEmailModelBase
{
    public required UserIdentification Initiator { get; set; }
}

/// <summary>
/// С проекта снята блокировка восстановления (ADR023) — уходит мастерам проекта и админам сайта.
/// </summary>
public class ProjectUnblockedMail : ProjectEmailModelBase
{
    public required UserInfoHeader Initiator { get; set; }
}

public class ProjectClosedStaleMail : ProjectEmailModelBase
{
    public required DateOnly LastActiveDate { get; set; }
}

public class ProjectStaleMail : ProjectEmailModelBase
{
    public required DateOnly LastActiveDate { get; set; }
    public required DateOnly WillCloseDate { get; set; }
}

/// <summary>
/// Еженедельное напоминание мастерам заблокированного проекта (ADR023) определиться с его судьбой:
/// закрыть проект или восстановить данные по инструкции.
/// </summary>
public class ProjectBlockedMail : ProjectEmailModelBase;
