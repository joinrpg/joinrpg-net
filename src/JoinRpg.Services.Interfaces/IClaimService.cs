using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Services.Interfaces;

public interface IClaimService
{
    Task<ClaimIdentification> AddClaimFromUser(CharacterIdentification
        characterId,
        string claimText,
        IReadOnlyDictionary<int, string?> fields,
        bool sensitiveDataAllowed);

    Task AddComment(ClaimIdentification claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText, FinanceOperationAction financeAction);

    Task ApproveByMaster(ClaimIdentification claimId, string commentText);
    Task DeclineByMaster(ClaimIdentification claimId, ClaimDenialReason claimDenialStatus, string commentText, bool deleteCharacter);
    Task DeclineByPlayer(ClaimIdentification claimId, string commentText);
    Task SetResponsible(ClaimIdentification claimId, UserIdentification responsibleMasterId);
    Task OnHoldByMaster(ClaimIdentification claimId, string commentText);

    Task RestoreByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId);

    Task MoveByMaster(ClaimIdentification claimId, string commentText, CharacterIdentification characterId);

    Task UpdateReadCommentWatermark(int projectId, int commentDiscussionId, int maxCommentId);

    Task SaveFieldsFromClaim(ClaimIdentification claimId, IReadOnlyDictionary<int, string?> newFieldValue);

    Task SubscribeClaimToUser(int projectId, int claimId);
    Task UnsubscribeClaimToUser(int projectId, int claimId);
    Task CheckInClaim(ClaimIdentification claimId, int money);
    Task<int> MoveToSecondRole(ClaimIdentification claimId, CharacterIdentification characterId, string secondRoleCommentText);

    Task<AccommodationRequest> SetAccommodationType(int projectId, int claimId, int accommodationTypeId);

    /// <summary>
    /// Excludes a claim from any accommodation group to a single occupation
    /// </summary>
    /// <param name="projectId">Database Id of a project</param>
    /// <param name="claimId">Database Id of a claim</param>
    /// <returns>
    /// null if claim is not accommodated
    /// <br />Existed accommodation request if there are no neighbours in accommodation.
    /// <br />New accommodation request if claim was in accommodation group.
    /// </returns>
    Task<AccommodationRequest?> LeaveAccommodationGroupAsync(int projectId, int claimId);

    Task ConcealComment(int projectId, int commentId, int commentDiscussionId, int currentUserId);
    Task AllowSensitiveData(ClaimIdentification projectId);
    Task<ClaimIdentification> SystemEnsureClaim(ProjectIdentification donateProjectId);
}

public enum FinanceOperationAction
{
    None,
    Approve,
    Decline,
}

