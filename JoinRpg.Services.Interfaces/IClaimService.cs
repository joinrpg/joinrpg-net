using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
  public interface IClaimService
  {
    Task AddClaimFromUser(int projectId, int? characterGroupId, int? characterId, int currentUserId, string claimText);

    Task AddComment(int projectId, int claimId, int currentUserId, int? parentCommentId, bool isVisibleToPlayer,
      string commentText, FinanceOperationAction financeAction);

    Task AppoveByMaster(int projectId, int claimId, int currentUserId, string commentText);
    Task DeclineByMaster(int projectId, int claimId, int currentUserId, string commentText);
    Task DeclineByPlayer(int projectId, int claimId, int currentUserId, string commentText);
    Task SetResponsible(int projectId, int claimId, int currentUserId, int responsibleMasterId);

    Task RestoreByMaster(int projectId, int claimId, int currentUserId, string commentText);

    Task MoveByMaster(int projectId, int claimId, int currentUserId, string contents, int? characterGroupId,
      int? characterId);

    void UpdateReadCommentWatermark(int projectId, int claimId, int currentUserId, int maxCommentId);
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