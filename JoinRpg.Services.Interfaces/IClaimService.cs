namespace JoinRpg.Services.Interfaces
{
  public interface IClaimService
  {
    void AddClaimFromUser(int projectId, int? characterGroupId, int? characterId, int currentUserId, string claimText);

    void AddComment(int projectId, int claimId, int currentUserId, int? parentCommentId, bool isVisibleToPlayer,
      bool isMyClaim, string commentText);
  }
}