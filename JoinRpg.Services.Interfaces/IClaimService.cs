using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
  public interface IClaimService
  {
    Task AddClaimFromUser(int projectId, int? characterGroupId, int? characterId, string claimText, IDictionary<int, string> fields);

    Task AddComment(int projectId, int claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText, FinanceOperationAction financeAction);

    Task AppoveByMaster(int projectId, int claimId, string commentText);
    Task DeclineByMaster(int projectId, int claimId, string commentText);
    Task DeclineByPlayer(int projectId, int claimId, string commentText);
    Task SetResponsible(int projectId, int claimId, int currentUserId, int responsibleMasterId);
    Task OnHoldByMaster(int projectId, int claimId, int currentUserId, string contents);

    Task RestoreByMaster(int projectId, int claimId, int currentUserId, string commentText);

    Task MoveByMaster(int projectId, int claimId, int currentUserId, string contents, int? characterGroupId,
      int? characterId);

    Task UpdateReadCommentWatermark(int projectId, int commentDiscussionId, int maxCommentId);

    Task SaveFieldsFromClaim(int projectId, int claimId, IDictionary<int, string> newFieldValue);

    Task SubscribeClaimToUser(int projectId, int claimId);
    Task UnsubscribeClaimToUser(int projectId, int claimId);
    Task CheckInClaim(int projectId, int claimId, int money);
    Task<int> MoveToSecondRole(int projectId, int claimId, int characterId);

    Task ConcealComment(int projectId, int commentId, int commentDiscussionId, int currentUserId);
  }

  //TODO[Localize]
  public enum FinanceOperationAction
  {
    [Display(Name = "Ничего не делать")]
    None,
    [Display(Name = "Подтвердить операцию")]
    Approve,
    [Display(Name = "Отменить операцию")]
    Decline
  }

}