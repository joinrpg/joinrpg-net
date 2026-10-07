namespace JoinRpg.Services.Interfaces.ProjectAccess;

/// <summary>
/// Профиль мастера на странице мастеров (ADR019, §4): роль, описание, видят ли его немастера.
/// Свой профиль мастер правит сам, чужой — с правом выдавать доступ.
/// </summary>
public class ChangeMasterProfileRequest
{
    public required ProjectIdentification ProjectId { get; init; }
    public required UserIdentification UserId { get; init; }
    public required MasterRoleTitle Role { get; init; }
    public MarkdownString? Description { get; init; }
    public required bool IsPublic { get; init; }
}
