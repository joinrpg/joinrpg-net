using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Interfaces;

public interface IClaimService
{
    Task<ClaimIdentification> AddClaimFromUser(CharacterIdentification
        characterId,
        string claimText,
        FieldLayerContainer fields,
        bool sensitiveDataAllowed);

    Task<ClaimIdentification> AddClaimFromMaster(CharacterIdentification characterId, UserIdentification userId, string commentText, FieldLayerContainer fields);

    /// <summary>Обычный комментарий к заявке.</summary>
    Task AddComment(ClaimIdentification claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText);

    /// <summary>
    /// Модерация финансовой операции, предложенной в комментарии <paramref name="parentCommentId"/>:
    /// одобрение или отклонение. Сопровождается комментарием мастера — всегда видимым игроку и
    /// не переводящим заявку в «обсуждается».
    /// </summary>
    Task ModerateFinanceOperation(ClaimIdentification claimId, int parentCommentId, string commentText, FinanceOperationAction financeAction);

    Task ApproveByMaster(ClaimIdentification claimId, string commentText);
    Task DeclineByMaster(ClaimIdentification claimId, ClaimDenialReason claimDenialStatus, string commentText, bool deleteCharacter);
    Task DeclineByPlayer(ClaimIdentification claimId, string commentText);
    Task SetResponsible(ClaimIdentification claimId, UserIdentification responsibleMasterId);
    Task OnHoldByMaster(ClaimIdentification claimId, string commentText);

    Task RestoreByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId);

    Task MoveByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId);

    Task UpdateReadCommentWatermark(int projectId, int commentDiscussionId, int maxCommentId);

    Task SaveFieldsFromClaim(ClaimIdentification claimId, FieldLayerContainer fieldsToSet);

    Task CheckInClaim(ClaimIdentification claimId, int money);
    Task<int> MoveToSecondRole(ClaimIdentification claimId, CharacterIdentification characterId, string secondRoleCommentText);

    /// <summary>
    /// Переводит заявку в новую одноместную группу проживающих выбранного типа. Если тип уже такой —
    /// ничего не делает.
    /// </summary>
    /// <remarks>
    /// Результата нет (ADR022 §4): <c>Id</c> новой группы до сохранения равен нулю, а группу заявки
    /// после операции даёт её доменный снимок — <c>CharacterClaimInfo.AccommodationGroupId</c>.
    /// </remarks>
    Task SetAccommodationType(int projectId, int claimId, int accommodationTypeId);

    /// <summary>
    /// Выводит заявку из группы проживающих в собственную одноместную группу того же типа. Если у
    /// заявки нет типа проживания или она в группе одна — ничего не делает.
    /// </summary>
    /// <param name="projectId">Database Id of a project</param>
    /// <param name="claimId">Database Id of a claim</param>
    Task LeaveAccommodationGroupAsync(int projectId, int claimId);

    Task ConcealComment(int projectId, int commentId, int commentDiscussionId);
    Task AllowSensitiveData(ClaimIdentification projectId);
    Task AcceptInvitation(ClaimIdentification claimId, string commentText, bool sensitiveDataAllowed);
    Task<ClaimIdentification> SystemEnsureClaim(ProjectIdentification donateProjectId);
}

public enum FinanceOperationAction
{
    None,
    Approve,
    Decline,
}

