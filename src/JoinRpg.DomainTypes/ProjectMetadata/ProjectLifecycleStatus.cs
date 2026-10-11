namespace JoinRpg.DomainTypes.ProjectMetadata;

public enum ProjectLifecycleStatus
{
    ActiveClaimsClosed,
    ActiveClaimsOpen,
    Archived,
}

/// <summary>
/// «Не в архиве» и «можно менять» — разные вопросы (ADR023): для чтения проект ведёт себя как активный,
/// пока он не в архиве, а менять его можно только в обычном рабочем состоянии.
/// </summary>
public static class ProjectLifecycleStatusExtensions
{
    /// <summary>Проект закрыт. Для чтения — сокращённые данные игроков, отдельные списки архивных проектов.</summary>
    public static bool IsArchived(this ProjectLifecycleStatus status) => status == ProjectLifecycleStatus.Archived;

    /// <summary>Проект можно менять обычными операциями: подавать заявки, править персонажей, поля и т. д.</summary>
    public static bool AllowsChanges(this ProjectLifecycleStatus status)
        => status is ProjectLifecycleStatus.ActiveClaimsOpen or ProjectLifecycleStatus.ActiveClaimsClosed;
}
