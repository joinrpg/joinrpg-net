using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Notification;

/// <summary>
/// Уведомления мастерам в проекте (вне привязок к конкретной заявке)
/// </summary>
public interface IMasterEmailService
{
    Task EmailProjectClosed(ProjectClosedMail email);
}

public class ProjectClosedMail : EmailModelBase
{
    public required ProjectIdentification ProjectId { get; set; }
}
