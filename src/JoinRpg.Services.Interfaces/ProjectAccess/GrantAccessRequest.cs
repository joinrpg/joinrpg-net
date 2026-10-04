namespace JoinRpg.Services.Interfaces.ProjectAccess;

public class GrantAccessRequest : AccessRequestBase
{
    /// <summary>
    /// Роль мастера в проекте (ADR019). Задаётся только новому мастеру; у того, кто уже мастер, не меняется.
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Показывать ли мастера немастерам (ADR019). Как и <see cref="Role"/>, только для нового мастера.
    /// </summary>
    public bool IsPublic { get; set; } = true;
}
