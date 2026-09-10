using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.DomainTypes.Users;

/// <summary>
/// Заявка пользователя в составе <see cref="UserInfo"/> — идентификатор и статус, без полей и
/// финансов.
/// </summary>
/// <remarks>
/// Нужен, чтобы правила заявки могли отвечать на вопрос «есть ли у игрока утверждённая заявка в
/// этом проекте», не поднимая заявки всего проекта из EF-графа.
/// </remarks>
public record class UserClaimInfo(ClaimIdentification ClaimId, ClaimStatus Status)
{
    public ProjectIdentification ProjectId => ClaimId.ProjectId;

    public bool IsApproved => Status is ClaimStatus.Approved or ClaimStatus.CheckedIn;

    public bool IsActive => Status.IsActive();
}
