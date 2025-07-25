namespace JoinRpg.Services.Interfaces.Notification;

/// <summary>
/// Уведомления мастерам в проекте (вне привязок к конкретной заявке)
/// </summary>
public interface IMasterEmailService
{
    Task EmailProjectClosed(ProjectClosedMail email);
    Task EmailProjectClosedStale(ProjectClosedStaleMail email);
    Task EmailProjectStale(ProjectStaleMail email);
}

public class ProjectEmailModelBase
{
    public required ProjectIdentification ProjectId { get; set; }
}

public class ProjectClosedMail : ProjectEmailModelBase
{
    public required UserIdentification Initiator { get; set; }
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
