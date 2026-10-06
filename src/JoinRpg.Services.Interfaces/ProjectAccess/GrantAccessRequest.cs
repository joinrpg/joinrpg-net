namespace JoinRpg.Services.Interfaces.ProjectAccess;

public class GrantAccessRequest : AccessRequestBase
{
    // Профиль (ADR019, §4) задаётся новому мастеру и тому, кого возвращают из бывших;
    // у действующего мастера выдача прав профиль не трогает — его правят отдельно.

    /// <summary>Роль мастера в проекте.</summary>
    public required MasterRoleTitle Role { get; set; }

    /// <summary>Чем мастер занимается и по каким вопросам ему писать.</summary>
    public MarkdownString? Description { get; set; }

    /// <summary>Показывать ли мастера немастерам.</summary>
    public bool IsPublic { get; set; } = true;
}
