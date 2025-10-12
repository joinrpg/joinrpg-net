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

    Task AddComment(int projectId, int claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText, FinanceOperationAction financeAction);

    Task ApproveByMaster(ClaimIdentification claimId, string commentText);
    Task DeclineByMaster(int projectId, int claimId, ClaimDenialReason claimDenialStatus, string commentText, bool deleteCharacter);
    Task DeclineByPlayer(int projectId, int claimId, string commentText);
    Task SetResponsible(int projectId, int claimId, int currentUserId, int responsibleMasterId);
    Task OnHoldByMaster(int projectId, int claimId, int currentUserId, string contents);

    Task RestoreByMaster(int projectId, int claimId, string commentText, int characterId);

    Task MoveByMaster(int projectId, int claimId, string commentText, int characterId);

    Task UpdateReadCommentWatermark(int projectId, int commentDiscussionId, int maxCommentId);

    Task SaveFieldsFromClaim(int projectId,
        int claimId,
        IReadOnlyDictionary<int, string?> newFieldValue);

    Task SubscribeClaimToUser(int projectId, int claimId);
    Task UnsubscribeClaimToUser(int projectId, int claimId);
    Task CheckInClaim(int projectId, int claimId, int money);
    Task<int> MoveToSecondRole(int projectId, int claimId, int characterId);

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

