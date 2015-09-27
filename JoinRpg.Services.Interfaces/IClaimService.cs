using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
  public interface IClaimService
  {
    Task AddClaimFromUser(int projectId, int? characterGroupId, int? characterId, int currentUserId, string claimText);

    Task AddComment(int projectId, int claimId, int currentUserId, int? parentCommentId, bool isVisibleToPlayer,
      bool isMyClaim, string commentText);

    Task AppoveByMaster(int projectId, int claimId, int currentUserId, string commentText);
    Task DeclineByMaster(int projectId, int claimId, int currentUserId, string commentText);
    Task DeclineByPlayer(int projectId, int claimId, int currentUserId, string commentText);
    Task SetResponsible(int projectId, int claimId, int currentUserId, int responsibleMasterId);
  }
}